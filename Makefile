CSC = mcs

CSVERSION = future

DEF = LINUX

BINDIR = ../bin

SRC = \
	$(SRCDIR)/Server.cs \
	$(SRCDIR)/Servlet.cs \
	$(SRCDIR)/Default404Servlet.cs \
	$(SRCDIR)/Properties/*.cs

DEBUGBINDIR = $(BINDIR)/Debug
RELEASEBINDIR = $(BINDIR)/Release

SYSLIBS = System.Data.dll,System.Core.dll,System.Web.dll
SQLLIB = Mono.Data.Sqlite.dll

TARGET = WebServer.dll

release:
	mkdir -p $(RELEASEBINDIR)
	rm -f $(RELEASEBINDIR)/$(TARGET)
	$(CSC) -langversion:$(CSVERSION) $(SRC) -r:$(SYSLIBS),$(SQLLIB) -d:$(DEF) \
		-out:$(RELEASEBINDIR)/$(TARGET) -target:library

debug:
	mkdir -p $(RELEASEBINDIR)
	rm -f $(RELEASEBINDIR)/$(TARGET)
	$(CSC) -langversion:$(CSVERSION) $(SRC) -r:$(SYSLIBS),$(SQLLIB) -d:$(DEF),DEBUG \
		-out:$(RELEASEBINDIR)/$(TARGET) -target:library
