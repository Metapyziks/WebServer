using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Mime;
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
    }

    public abstract class FormField
    {
        private static String GetFormatExceptionMessage(int code)
        {
            return String.Format("Badly formatted form field (0x{0:x2}).", code);
        }

        public static FormField Create(IDictionary<String, FormFieldHeader> headers, Stream stream)
        {
            var ctype = headers["Content-Type"];
            if (ctype != null && ctype.Value.StartsWith("multipart/", StringComparison.InvariantCultureIgnoreCase)) {
                return new MultipartFormField(headers, stream);
            }

            var cdisp = headers["Content-Disposition"];
            if (cdisp == null) {
                throw new HttpException(400, GetFormatExceptionMessage(0x00));
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

        private readonly Dictionary<String, FormFieldHeader> _headers;

        public String Name { get; private set; }

        public String ContentType { get; private set; }

        public String Boundary { get; private set; }

        public abstract bool IsMultipart { get; }

        public abstract bool IsFile { get; }

        public abstract bool IsText { get; }

        public TransferEncoding Encoding { get; private set; }

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
                _headers = new Dictionary<string, FormFieldHeader>(headers, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        public HeaderCollection Headers { get; private set; }

        public FormField(IDictionary<String, FormFieldHeader> headers)
        {
            Headers = new HeaderCollection(headers);

            var cdisp = Headers["Content-Disposition"];
            if (cdisp != null) Name = cdisp["name"];

            var ctype = Headers["Content-Type"];
            if (ctype != null) {
                ContentType = ctype.Value;
                Boundary = ctype["boundary"];
            }

            var tenc = Headers["Content-Transfer-Encoding"];
            if (tenc != null) {
                switch (tenc.Value.ToLower()) {
                    case "base64":
                        Encoding = TransferEncoding.Base64;
                        break;
                    case "8bit":
                        Encoding = TransferEncoding.EightBit;
                        break;
                    case "7bit":
                        Encoding = TransferEncoding.SevenBit;
                        break;
                    default:
                        Encoding = TransferEncoding.Unknown;
                        IsBinary = true;
                        break;
                }
            } else {
                Encoding = TransferEncoding.SevenBit;
            }
        }
    }

    public sealed class MultipartFormField : FormField, IEnumerable<FormField>
    {
        public override bool IsMultipart { get { return true; } }

        public override bool IsFile { get { return false; } }

        public override bool IsText { get { return false; } }

        private readonly FormField[] _subFields;

        public FormField this[String headerKey]
        {
            get { return _subFields.FirstOrDefault(x => x.Name == headerKey); }
        }

        private static String GetFormatExceptionMessage(int code, String data = null)
        {
            return String.Format("Badly formatted 'multipart/form-data' (0x{0:x2}).{1}", code,
                data == null ? String.Empty : Environment.NewLine + data);
        }

        public MultipartFormField(IDictionary<String, FormFieldHeader> headers, Stream stream)
            : base(headers)
        {
            var reader = new StreamReader(stream, System.Text.Encoding.ASCII, false, 128, true);
            var headerDict = new Dictionary<String, FormFieldHeader>();

            var subFields = new List<FormField>();

            var line = reader.ReadLine();
            while (true) {
                if (line == null || !line.StartsWith(String.Format("--{0}", Boundary))) {
                    throw new HttpException(400, GetFormatExceptionMessage(0x10, line));
                }

                if (line.EndsWith("--")) break;

                headerDict.Clear();

                String headerLine;
                while (!String.IsNullOrWhiteSpace(headerLine = reader.ReadLine())) {
                    var keyVal = FormFieldHeader.ParseKeyValue(headerLine);
                    if (keyVal.Key == null || keyVal.Value == null) {
                        throw new HttpException(400, GetFormatExceptionMessage(0x11, line));
                    }

                    headerDict.Add(keyVal.Key, keyVal.Value);
                }

                var start = reader.BaseStream.Position;
                var end = reader.BaseStream.Position;
                while ((line = reader.ReadLine()) != null) {
                    if (!line.StartsWith(String.Format("--{0}", Boundary))) {
                        end = reader.BaseStream.Position;
                        continue;
                    }

                    subFields.Add(Create(headerDict, new FrameStream(stream, start, end - start)));
                }
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

        public FileFormField(IDictionary<String, FormFieldHeader> headers, Stream stream)
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

        public TextFormField(IDictionary<String, FormFieldHeader> headers, Stream stream)
            : base(headers)
        {
            Value = new StreamReader(stream).ReadToEnd();
        }
    }
}
