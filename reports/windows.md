# CI report: windows
- status: failure
- workflow: Build Windows (BuduPing)
- run: 39292875790/48
- sha: b85be07e698efe1abee103273411d45e134e71d0
- time: 2026-10-19T17:19:27Z

## build log (tail 300)
```
  Determining projects to restore...
  Restored D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj (in 9.84 sec).
D:\a\BuduPing\BuduPing\windows\BuduPing\Core\AppConfig.cs(76,15): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'. [D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj]
  BuduPing -> D:\a\BuduPing\BuduPing\windows\BuduPing\bin\Release\net8.0-windows\win-x64\BuduPing.dll
  BuduPing -> D:\a\BuduPing\BuduPing\windows\BuduPing\publish\
```
