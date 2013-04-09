[CustomMessages]
dokanlib_title=Dokan Library 6.0
dokanlib_size=0 MB

[Code]
procedure dokanlib();
var
	version: cardinal;
	
begin
	if not FileExists(ExpandConstant('{win}\Sysnative\Drivers\dokan.sys')) and not FileExists(ExpandConstant('{sys}\Drivers\dokan.sys')) then
		AddProduct('DokanInstall_0.6.0.exe',
			'/S',
			CustomMessage('dokanlib_title'),
			CustomMessage('dokanlib_size'),
			'http://google.com');
end;