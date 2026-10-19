# CI report: windows
- status: failure
- workflow: Build Windows (BuduPing)
- run: 39298929205/50
- sha: ee944460e3e5f63d71ab92b2f500dbaea302bb0f
- time: 2026-10-19T18:02:17Z

## build log (tail 300)
```
  Determining projects to restore...
  Restored D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj (in 5.63 sec).
D:\a\BuduPing\BuduPing\windows\BuduPing\Core\AppConfig.cs(76,15): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'. [D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj]
  BuduPing -> D:\a\BuduPing\BuduPing\windows\BuduPing\bin\Release\net8.0-windows\win-x64\BuduPing.dll
  BuduPing -> D:\a\BuduPing\BuduPing\windows\BuduPing\publish\
```
