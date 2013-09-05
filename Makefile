CSC = mcs

CSVERSION = future

DEF = LINUX

BINDIR = ../bin

SRC = \
	./*.cs \
	Properties/*.cs

DEBUGBINDIR = $(BINDIR)/Debug
RELEASEBINDIR = $(BINDIR)/Release

TARGET = WebServer.dll

release:
	mkdir -p $(RELEASEBINDIR)
	rm -f $(RELEASEBINDIR)/$(TARGET)
	$(CSC) -langversion:$(CSVERSION) $(SRC) -d:$(DEF) \
		-out:$(RELEASEBINDIR)/$(TARGET) -target:library

debug:
	mkdir -p $(RELEASEBINDIR)
	rm -f $(RELEASEBINDIR)/$(TARGET)
	$(CSC) -langversion:$(CSVERSION) $(SRC) -r:$(SYSLIBS),$(SQLLIB) -d:$(DEF),DEBUG \
		-out:$(RELEASEBINDIR)/$(TARGET) -target:library
