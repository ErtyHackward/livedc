#include "scripts\products.iss"
#include "scripts\products\dotnetfx40.iss"
#include "scripts\products\dokanlib.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"

[Languages]
Name: en; MessagesFile: compiler:Default.isl
Name: ru; MessagesFile: compiler:Languages\Russian.isl
Name: fr; MessagesFile: compiler:Languages\French.isl

[Files]
Source: Deps\DokanInstall_0.6.0.exe; Flags: dontcopy
Source: ..\LiveDC\bin\Release\*; DestDir: {app}; Excludes: *.dds,*.pdb,*.vshost.*,*.exe.config,Newtonsoft.Json.xml,Ninject.xml,NLog.xml,protobuf-net.xml,SharpDX.*.xml,SharpDX.xml,*.exp,*.lib,\Plugins\*; Flags: ignoreversion recursesubdirs createallsubdirs
;Source: Deps\PTN57F.ttf; DestDir: {fonts}; FontInstall: "PT Sans Narrow"; Flags: onlyifdoesntexist uninsneveruninstall
;Source: Deps\PTN77F.ttf; DestDir: {fonts}; FontInstall: "PT Sans Narrow Bold"; Flags: onlyifdoesntexist uninsneveruninstall

[Setup]
VersionInfoVersion=1.1.11
VersionInfoCompany=LiveDC
VersionInfoDescription=LiveDC
VersionInfoCopyright=Vladislav Pozdnyakov, 2013
VersionInfoProductName=LiveDC
MinVersion=0,5.01.2600sp1
AppName=LiveDC
AppVerName=LiveDC, 1.1.11
AppPublisher=April32
AppPublisherURL=http://april32.com
AppSupportURL=http://april32.com
AppUpdatesURL=http://livedc.april32.com
LicenseFile=EULA.txt
DisableProgramGroupPage=true
ShowLanguageDialog=auto
DefaultDirName={pf}\LiveDC\
;SetupIconFile=..\Media\icon.ico
;WizardImageFile=setupTitle.bmp
;WizardSmallImageFile=setupSmall.bmp
;UninstallDisplayIcon=..\Media\icon.ico
OutputBaseFilename=setup_livedc
;ArchitecturesInstallIn64BitMode=x64

[Icons]
Name: {group}\LiveDC; Filename: {app}\LiveDC.exe

[Run]
Filename: {app}\LiveDC.exe; Parameters: -createshortcut; Description: {cm:CreateDesktopIcon,LiveDC}; Flags: postinstall
Filename: {app}\LiveDC.exe; Parameters: -setstartup; Description: {cm:AutoStartProgram,LiveDC}; Flags: postinstall
Filename: {app}\LiveDC.exe; Description: {cm:LaunchProgram,LiveDC}; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
	dotnetfx40();
	ExtractTemporaryFile('DokanInstall_0.6.0.exe');
	dokanlib();
	Result := true;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
	ProcessMsgPage: TOutputProgressWizardPage;
	ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    	ProcessMsgPage := CreateOutputProgressPage(CustomMessage('postprocess'),CustomMessage('postprocess'));
		ProcessMsgPage.Show;

		Exec('netsh', ExpandConstant('advfirewall firewall add rule name="LiveDC" dir=in action=allow program="{app}\LiveDC.exe" enable=yes'),'', SW_HIDE, ewWaitUntilTerminated, ResultCode);

		ProcessMsgPage.Hide;
  end;
end;
