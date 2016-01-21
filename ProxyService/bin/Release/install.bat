%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe ProxyService.exe
Net Start ProxyService
sc config ProxyService start= auto
pause