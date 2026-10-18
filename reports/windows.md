# CI report: windows
- status: failure
- workflow: Build Windows (BuduPing)
- run: 39153263708/10
- sha: f26cd0c4751f4466de3c6caebaf904aafd455b5c
- time: 2026-10-18T18:15:53Z

## build log (tail 300)
```
MSBUILD : error MSB1008: Only one project can be specified.
    Full command line: 'C:\Program Files\dotnet\sdk\10.0.401\MSBuild.exe -maxcpucount --verbosity:m -tlp:default=auto --property:PublishDir=D:\a\BuduPing\BuduPing\windows\BuduPing\publish --property:_CommandLineDefinedOutputPath=true --property:SelfContained=true --property:_CommandLineDefinedSelfContained=true --property:Configuration=Release --property:NuGetInteractive=false --property:RuntimeIdentifier=win-x64 --property:_CommandLineDefinedRuntimeIdentifier=true --property:_IsPublishing=true --property:DOTNET_CLI_DISABLE_PUBLISH_AND_PACK_RELEASE=true --restoreProperty:PublishDir=D:\a\BuduPing\BuduPing\windows\BuduPing\publish --restoreProperty:_CommandLineDefinedOutputPath=true --restoreProperty:SelfContained=true --restoreProperty:_CommandLineDefinedSelfContained=true --restoreProperty:Configuration=Release --restoreProperty:NuGetInteractive=false --restoreProperty:RuntimeIdentifier=win-x64 --restoreProperty:_CommandLineDefinedRuntimeIdentifier=true --restoreProperty:_IsPublishing=true --restoreProperty:DOTNET_CLI_DISABLE_PUBLISH_AND_PACK_RELEASE=true --restoreProperty:EnableDefaultCompileItems=false --restoreProperty:EnableDefaultEmbeddedResourceItems=false --restoreProperty:EnableDefaultNoneItems=false --target:Publish --nologo p:PublishSingleFile=true p:IncludeNativeLibrariesForSelfExtract=true -restore -distributedlogger:Microsoft.DotNet.Cli.Commands.MSBuild.MSBuildLogger,C:\Program Files\dotnet\sdk\10.0.401\dotnet.dll*Microsoft.DotNet.Cli.Commands.MSBuild.MSBuildForwardingLogger,C:\Program Files\dotnet\sdk\10.0.401\dotnet.dll'
  Switches appended by response files:
Switch: p:IncludeNativeLibrariesForSelfExtract=true

For switch syntax, type "MSBuild -help"
```
