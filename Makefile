CSC = mcs

CSVERSION = future

DEF = LINUX

BINDIR = ../bin

SRC = \
	./*.cs \
	Properties/*.cs

SYSLIBS = System.Data.dll,System.Core.dll,System.Web.dll

DEBUGBINDIR = $(BINDIR)/Debug
RELEASEBINDIR = $(BINDIR)/Release

TARGET = WebServer.dll

release:
	mkdir -p $(RELEASEBINDIR)
	rm -f $(RELEASEBINDIR)/$(TARGET)
	$(CSC) -langversion:$(CSVERSION) $(SRC) -r:$(SYSLIBS) -d:$(DEF) \
		-out:$(RELEASEBINDIR)/$(TARGET) -target:library

debug:
	mkdir -p $(RELEASEBINDIR)
	rm -f $(RELEASEBINDIR)/$(TARGET)
	$(CSC) -langversion:$(CSVERSION) $(SRC) -r:$(SYSLIBS) -d:$(DEF),DEBUG \
		-out:$(RELEASEBINDIR)/$(TARGET) -target:library
