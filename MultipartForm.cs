﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace WebServer
{
    public class FormFieldHeader
    {
        private static readonly Regex _sHeaderRegex = new Regex(
            @"^\s*(?<key>[A-Z-]+)\s*:" +
            @"\s*(""(?<value>[^""]+)""|(?<value>[^;]*))" +
            @"\s*(;\s*(?<opkey>[A-Z-]+)\s*=" +
            @"\s*(""(?<opvalue>[^""]+)""|(?<opvalue>[^;]*))\s*)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static FormFieldHeader Parse(String line)
        {
            return ParseKeyValue(String.Format("--: {0}", line)).Value;
        }

        public static KeyValuePair<String, FormFieldHeader> ParseKeyValue(String line)
        {
            var match = _sHeaderRegex.Match(line);
            if (!match.Success) return new KeyValuePair<String, FormFieldHeader>(null, null);

            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;
            var options = new NameValueCollection();

            var opKeys = match.Groups["opkey"].Captures;
            var opValues = match.Groups["opvalue"].Captures;

            var optionCount = opKeys.Count;
            for (var i = 0; i < optionCount; ++i) {
                var opKey = opKeys[i].Value;
                var opValue = opValues[i].Value;
                options.Add(opKey, opValue);
            }

            return new KeyValuePair<String, FormFieldHeader>(key, new FormFieldHeader(value, options));
        }

        public String Value { get; private set; }
        public NameValueCollection Options { get; private set; }

        public String this[String option]
        {
            get { return Options[option]; }
        }

        private FormFieldHeader(String value, NameValueCollection options)
        {
            Value = value;
            Options = options;
        }

        public override string ToString()
        {
            return Value + String.Join(String.Empty, Options.AllKeys.Select(x => String.Format("; {0}=\"{1}\"", x, Options[x])));
        }
    }

    public abstract class FormField
    {
        private static String GetFormatExceptionMessage(int code)
        {
            return String.Format("Badly formatted form field (0x{0:x2}).", code);
        }

        public static FormField Create(IDictionary<String, FormFieldHeader> headerDict, Stream stream)
        {
            var headers = new HeaderCollection(headerDict);

            var ctype = headers["Content-Type"];
            if (ctype != null && ctype.Value.StartsWith("multipart/", StringComparison.InvariantCultureIgnoreCase)) {
                return new MultipartFormField(headers, stream);
            }

            var cdisp = headers["Content-Disposition"];
            if (cdisp == null) {
                throw new HttpException(400, GetFormatExceptionMessage(0x00));
            }

            if (cdisp["filename"] != null) {
                return new FileFormField(headers, stream);
            }

            switch (cdisp.Value.ToLower()) {
                case "file":
                    return new FileFormField(headers, stream);
                case "form-data":
                    return new TextFormField(headers, stream);
                default:
                    throw new HttpException(400, GetFormatExceptionMessage(0x01));
            }
        }

        public String Name { get; private set; }

        public String ContentType { get; private set; }

        public String Boundary { get; private set; }

        public abstract bool IsMultipart { get; }

        public abstract bool IsFile { get; }

        public abstract bool IsText { get; }

        public bool IsBinary { get; private set; }

        public class HeaderCollection
        {
            private readonly IDictionary<String, FormFieldHeader> _headers;

            public FormFieldHeader this[String key]
            {
                get { return _headers.ContainsKey(key) ? _headers[key] : null; }
            }

            internal HeaderCollection(IDictionary<String, FormFieldHeader> headers)
            {
                _headers = new Dictionary<String, FormFieldHeader>(headers, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        public HeaderCollection Headers { get; private set; }

        public FormField(HeaderCollection headers)
        {
            Headers = headers;

            var cdisp = Headers["Content-Disposition"];
            if (cdisp != null) { Name = cdisp["name"]; }

            var ctype = Headers["Content-Type"];
            if (ctype != null) {
                ContentType = ctype.Value;
                Boundary = ctype["boundary"];

                if (ContentType == "application/octet-stream") {
                    IsBinary = true;
                }
            }

            var tenc = Headers["Content-Transfer-Encoding"];
            if (tenc != null) {
                switch (tenc.Value.ToLower()) {
                    case "base64":
                    case "8bit":
                    case "7bit":
                        break;
                    default:
                        IsBinary = true;
                        break;
                }
            }
        }
    }

    public sealed class MultipartFormField : FormField, IEnumerable<FormField>
    {
        public override bool IsMultipart { get { return true; } }

        public override bool IsFile { get { return false; } }

        public override bool IsText { get { return false; } }

        private readonly FormField[] _subFields;

        public IEnumerable<String> FieldNames
        {
            get
            {
                return _subFields
                    .Select(x => x.Headers["Content-Disposition"])
                    .Where(x => x != null)
                    .Select(x => x.Options["name"])
                    .Where(x => !String.IsNullOrWhiteSpace(x));
            }
        }

        public FormField this[String headerKey]
        {
            get { return _subFields.FirstOrDefault(x => x.Name == headerKey); }
        }

        private static String GetFormatExceptionMessage(int code, String format = null, params Object[] args)
        {
            return String.Format("Badly formatted 'multipart/form-data' (0x{0:x2}).{1}", code,
                format == null ? String.Empty : Environment.NewLine + (args.Length == 0 ? format : String.Format(format, args)));
        }

        private static String ReadLine(Stream stream, byte[] buffer)
        {
            var line = String.Empty;

            var start = stream.Position;

            var total = 0;
            var prev = '\0';

            int read;
            while ((read = stream.Read(buffer, 0, 128)) > 0) {
                int i;
                for (i = 0; i < read; ++i) {
                    var curr = (char) buffer[i];
                    if (prev == '\r' && curr == '\n') {
                        --i; break;
                    }
                    prev = curr;
                }

                total += i;

                line = i < 0
                    ? line.Substring(0, line.Length - 1)
                    : String.Concat(line, Encoding.ASCII.GetString(buffer, 0, i));

                if (i < read) break;
            }

            if (stream.Position == start) return null;

            stream.Seek(Math.Min(stream.Length, start + total + 2), SeekOrigin.Begin);

            return line;
        }

        public MultipartFormField(HeaderCollection headers, Stream stream)
            : base(headers)
        {
            var headerDict = new Dictionary<String, FormFieldHeader>();
            var subFields = new List<FormField>();

            var readlineBuffer = new byte[128];

            var line = ReadLine(stream, readlineBuffer);
            while (true) {
                if (line == null || !line.StartsWith(String.Format("--{0}", Boundary))) {
                    throw new HttpException(400, GetFormatExceptionMessage(0x10));
                }

                if (line.EndsWith("--")) break;

                headerDict.Clear();

                String headerLine;
                while (!String.IsNullOrWhiteSpace(headerLine = ReadLine(stream, readlineBuffer))) {
                    var keyVal = FormFieldHeader.ParseKeyValue(headerLine);
                    if (keyVal.Key == null || keyVal.Value == null) {
                        throw new HttpException(400, GetFormatExceptionMessage(0x11));
                    }

                    headerDict.Add(keyVal.Key, keyVal.Value);
                }

                var start = stream.Position;
                var end = stream.Position;

                while ((line = ReadLine(stream, readlineBuffer)) != null) {
                    if (line.StartsWith(String.Format("--{0}", Boundary))) {
                        break;
                    }

                    end = stream.Position;
                }

                stream.Seek(start, SeekOrigin.Begin);
                var field = Create(headerDict, new FrameStream(stream, start, end - start - 2));
                stream.Seek(end, SeekOrigin.Begin);

                ReadLine(stream, readlineBuffer);

                subFields.Add(field);
            }

            _subFields = subFields.ToArray();
        }

        public IEnumerator<FormField> GetEnumerator()
        {
            return _subFields.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _subFields.GetEnumerator();
        }
    }

    public sealed class FileFormField : FormField
    {
        public byte[] Data { get; private set; }

        public override bool IsMultipart { get { return false; } }

        public override bool IsFile { get { return true; } }

        public override bool IsText { get { return false; } }

        public FileFormField(HeaderCollection headers, Stream stream)
            : base(headers)
        {
            Data = new byte[stream.Length];
            stream.Read(Data, 0, Data.Length);
        }
    }

    public sealed class TextFormField : FormField
    {
        public String Value { get; private set; }

        public override bool IsMultipart { get { return false; } }

        public override bool IsFile { get { return false; } }

        public override bool IsText { get { return true; } }

        public TextFormField(HeaderCollection headers, Stream stream)
            : base(headers)
        {
            Value = new StreamReader(stream).ReadToEnd();
        }
    }
}
