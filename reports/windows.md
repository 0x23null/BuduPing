# CI report: windows
- status: failure
- workflow: Build Windows (BuduPing)
- run: 39304409234/54
- sha: 4d9c83140c0e7b3a5791182502ef4c5111547492
- time: 2026-10-19T18:42:02Z

## build log (tail 300)
```
  Determining projects to restore...
  Restored D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj (in 7.49 sec).
D:\a\BuduPing\BuduPing\windows\BuduPing\Core\AppConfig.cs(76,15): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'. [D:\a\BuduPing\BuduPing\windows\BuduPing\BuduPing.csproj]
  BuduPing -> D:\a\BuduPing\BuduPing\windows\BuduPing\bin\Release\net8.0-windows\win-x64\BuduPing.dll
  BuduPing -> D:\a\BuduPing\BuduPing\windows\BuduPing\publish\
**********************
PowerShell transcript start
Start time: 20261019184150
Username: runnervm0xl3a\runneradmin
RunAs User: runnervm0xl3a\runneradmin
Configuration Name: 
Machine: runnervm0xl3a (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\8365c88a-a85d-4af5-aab3-e3f10f0a75ef.ps1'
Process ID: 1376
PSVersion: 7.6.6
PSEdition: Core
GitCommitId: 7.6.6
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************
System.InvalidOperationException: 200-tap spam did not remain bounded.
   at BuduPing.App.<>c__DisplayClass33_0.<<RenderUiReview>b__0>d.MoveNext() in D:\a\BuduPing\BuduPing\windows\BuduPing\App.xaml.cs:line 161
PS>TerminatingError(): "UI review process failed"
>> TerminatingError(): "UI review process failed"
Exception: D:\a\_temp\8365c88a-a85d-4af5-aab3-e3f10f0a75ef.ps1:9
Line |
   9 |    throw 'UI review process failed'
     |    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | UI review process failed

PS>$global:?
False
**********************
PowerShell transcript end
End time: 20261019184159
**********************
```
