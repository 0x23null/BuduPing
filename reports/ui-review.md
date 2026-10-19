# CI report: ui-review
- status: failure
- workflow: Android UI review
- run: 39301574172/18
- sha: 273347f7eac4e496dcff97e27c9fc9493bd19fb9
- time: 2026-10-19T18:26:51Z

## build log (tail 300)
```
Downloading https://services.gradle.org/distributions/gradle-8.7-bin.zip
............10%.............20%.............30%.............40%............50%.............60%.............70%.............80%.............90%............100%

Welcome to Gradle 8.7!

Here are the highlights of this release:
 - Compiling and testing with Java 22
 - Cacheable Groovy script compilation
 - New methods in lazy collection properties

For more details see https://docs.gradle.org/8.7/release-notes.html

To honour the JVM settings for this build a single-use Daemon process will be forked. For more on this, please refer to https://docs.gradle.org/8.7/userguide/gradle_daemon.html#sec:disabling_the_daemon in the Gradle documentation.
Daemon will be stopped at the end of the build 
> Task :app:checkKotlinGradlePluginConfigurationErrors SKIPPED
> Task :app:preBuild UP-TO-DATE
> Task :app:preDebugBuild UP-TO-DATE
> Task :app:generateDebugResValues
> Task :app:checkDebugAarMetadata
> Task :app:processDebugGoogleServices
> Task :app:mapDebugSourceSetPaths
> Task :app:generateDebugResources
> Task :app:packageDebugResources
> Task :app:mergeDebugResources
> Task :app:createDebugCompatibleScreenManifests
> Task :app:extractDeepLinksDebug
> Task :app:parseDebugLocalResources
> Task :app:processDebugMainManifest
> Task :app:processDebugManifest
> Task :app:processDebugManifestForPackage
> Task :app:javaPreCompileDebug
> Task :app:preDebugAndroidTestBuild SKIPPED
> Task :app:processDebugResources
> Task :app:checkDebugAndroidTestAarMetadata
> Task :app:generateDebugAndroidTestResValues
> Task :app:mapDebugAndroidTestSourceSetPaths
> Task :app:generateDebugAndroidTestResources
> Task :app:mergeDebugAndroidTestResources
> Task :app:processDebugAndroidTestManifest
> Task :app:processDebugAndroidTestResources
> Task :app:javaPreCompileDebugAndroidTest
> Task :app:mergeDebugShaders
> Task :app:compileDebugShaders NO-SOURCE
> Task :app:generateDebugAssets UP-TO-DATE
> Task :app:mergeDebugAssets
> Task :app:compressDebugAssets
> Task :app:desugarDebugFileDependencies
> Task :app:checkDebugDuplicateClasses
> Task :app:mergeDebugStartupProfile

> Task :app:compileDebugKotlin
e: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:223:30 None of the following candidates is applicable:
fun Modifier.padding(paddingValues: PaddingValues): Modifier
fun Modifier.padding(all: Dp): Modifier
fun Modifier.padding(horizontal: Dp = ..., vertical: Dp = ...): Modifier
fun Modifier.padding(start: Dp = ..., top: Dp = ..., end: Dp = ..., bottom: Dp = ...): Modifier
e: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:223:74 Syntax error: Unexpected tokens (use ';' to separate expressions on the same line).
e: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:224:29 @Composable invocations can only happen from the context of a @Composable function
e: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:224:77 @Composable invocations can only happen from the context of a @Composable function

> Task :app:mergeExtDexDebug
> Task :app:compileDebugKotlin FAILED

FAILURE: Build failed with an exception.

* What went wrong:
Execution failed for task ':app:compileDebugKotlin'.
> A failure occurred while executing org.jetbrains.kotlin.compilerRunner.GradleCompilerRunnerWithWorkers$GradleKotlinCompilerWorkAction
   > Compilation error. See log for more details

* Try:
> Run with --stacktrace option to get the stack trace.
> Run with --info or --debug option to get more log output.
> Run with --scan to get full insights.
> Get more help at https://help.gradle.org.

BUILD FAILED in 5m 9s
31 actionable tasks: 31 executed
```
