signtool sign /v /f MySPC.pfx /t "http://timestamp.verisign.com/scripts/timstamp.dll" output\setup_livedc.exe
signtool verify /pa output\setup_livedc.exe
pause