REM add root cert: "certutil -user -addstore Root OwnCA.cer"
%~dp0signtool sign /v /f %~dp0MySPC.pfx /t "http://timestamp.verisign.com/scripts/timstamp.dll" %~dp0output\setup_livedc.exe
%~dp0signtool verify /pa %~dp0output\setup_livedc.exe
pause