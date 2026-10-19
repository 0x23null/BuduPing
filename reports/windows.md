# CI report: windows
- status: failure
- workflow: Build Windows (BuduPing)
- run: 39264483568/33
- sha: b2fa21ae57a0de1e98678a8a6336664bb41fda1b
- time: 2026-10-19T13:57:56Z

## build log (tail 300)
```
  Determining projects to restore...
  Restored D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj (in 5.95 sec).
D:\a\BuduPing\BuduPing\windows\BuduPing\Ui\OverlayWindow.xaml.cs(445,9): error CS0103: The name 'ImageFallback' does not exist in the current context [D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj]
D:\a\BuduPing\BuduPing\windows\BuduPing\Ui\OverlayWindow.xaml.cs(463,13): error CS0103: The name 'ImageFallback' does not exist in the current context [D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj]
D:\a\BuduPing\BuduPing\windows\BuduPing\Core\AppConfig.cs(76,15): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'. [D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj]
```
