# CI report: ui-review
- status: failure
- workflow: Android UI review
- run: 39158537853/6
- sha: 8f6c51e6305f727eeb4325f7ec6456f27237fd0d
- time: 2026-10-18T19:27:26Z

## build log (tail 300)
```

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
> Task :app:parseDebugLocalResources
> Task :app:mergeDebugResources
> Task :app:createDebugCompatibleScreenManifests
> Task :app:extractDeepLinksDebug
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
e: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:55:33 Unresolved reference 'catalog'.
e: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:57:36 Unresolved reference 'catalog'.

> Task :app:compileDebugKotlin FAILED
> Task :app:mergeExtDexDebug

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

BUILD FAILED in 1m 36s
31 actionable tasks: 31 executed
```
