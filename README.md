# Stampfer
Überarbeitete Version von Sumpfkraufjunkies "Stampfer" - eine Entwicklungsumgebung für Daedalus (Gothic I &amp; II) und mehr

Als basis für diese Version dient der letzte verfügbare Quellcode von Stampfer.  
Das PeterInterface wurde mithilfe von dnSpy Dekompiliert und als Projekt exportiert um die Abhängigkeiten zu aktualisieren

![Stampfer](https://puu.sh/DoHFf.png)

Voraussetztungen:
- .NET Framework 4.5 (Windows 7+)

Neuerungen:
- Erstmaliger Start erzeugt nun alle benötigten Dateien und Verzeichnisse
  - Es wird beim Start nach dem "Scripts"-Verzeichnis gefragt, wenn dieses nicht in der Config.dat hinterlegt ist.
- Es wird eine "Standard"-Config.dat angelegt
- Aktualisierte Abhängigkeiten
- Kleinere Performanceverbesserungen


## Config.dat
```xml
<?xml version="1.0"?>
<PeterConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Application>
    <SaveOnExit>false</SaveOnExit>
    <Top>-8</Top>
    <Left>-8</Left>
    <Width>1936</Width>
    <Height>1056</Height>
    <RecentFileCount>10</RecentFileCount>
    <RecentProjectCount>5</RecentProjectCount>
  </Application>
  <RecentProjects />
  <RecentFiles>
    <file>A:\Spiele\Gothic II_Mods\_work\Data\Scripts\Content\Ninja_ManaReg\_INTERN\Ninja_ManaReg_REGENERATION.d</file>
  </RecentFiles>
  <Editor>
    <ShowEOL>false</ShowEOL>
    <ShowInvalidLines>false</ShowInvalidLines>
    <ShowSpaces>false</ShowSpaces>
    <ShowTabs>false</ShowTabs>
    <ShowMatchBracket>true</ShowMatchBracket>
    <ShowLineNumbers>true</ShowLineNumbers>
    <ShowHRuler>false</ShowHRuler>
    <ShowVRuler>false</ShowVRuler>
    <EnableCodeFolding>true</EnableCodeFolding>
    <ConvertTabs>true</ConvertTabs>
    <UseAntiAlias>false</UseAntiAlias>
    <AllowCaretBeyondEOL>false</AllowCaretBeyondEOL>
    <HighlightCurrentLine>true</HighlightCurrentLine>
    <AutoInsertBracket>true</AutoInsertBracket>
    <TabIndent>4</TabIndent>
    <VerticalRulerCol>90</VerticalRulerCol>
    <IndentStyle>auto</IndentStyle>
    <BracketMatchingStyle>Nachher</BracketMatchingStyle>
    <Font>Courier New;9,75</Font>
    <Scripts>A:\Spiele\Gothic II_Mods\_work\Data\Scripts</Scripts>
    <Bilder />
    <parser>false</parser>
    <backup>true</backup>
    <autocomplete>true</autocomplete>
    <backupfolder />
    <backupeach>0</backupeach>
    <backupfolderonly>false</backupfolderonly>
    <autobrackets>true</autobrackets>
  </Editor>
</PeterConfig>
```
