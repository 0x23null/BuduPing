# CI report: ui-review
- status: failure
- workflow: Android UI review
- run: 39288747414/15
- sha: 47ebda8530552d9b243416704ab7f8d703aafc5d
- time: 2026-10-19T16:55:49Z

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
> Task :app:javaPreCompileDebug
> Task :app:processDebugManifestForPackage
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
w: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/HomeScreen.kt:99:59 'static field VIBRATOR_SERVICE: String' is deprecated. Deprecated in Java.
w: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/PairScreen.kt:71:47 'constructor(capitalization: KeyboardCapitalization = ..., autoCorrect: Boolean, keyboardType: KeyboardType = ..., imeAction: ImeAction = ..., platformImeOptions: PlatformImeOptions? = ..., showKeyboardOnFocus: Boolean? = ..., hintLocales: LocaleList? = ...): KeyboardOptions' is deprecated. Please use the new constructor that takes optional autoCorrectEnabled parameter.
w: file:///home/runner/work/BuduPing/BuduPing/android/app/src/main/java/com/buduping/app/ui/PairScreen.kt:77:47 'constructor(capitalization: KeyboardCapitalization = ..., autoCorrect: Boolean, keyboardType: KeyboardType = ..., imeAction: ImeAction = ..., platformImeOptions: PlatformImeOptions? = ..., showKeyboardOnFocus: Boolean? = ..., hintLocales: LocaleList? = ...): KeyboardOptions' is deprecated. Please use the new constructor that takes optional autoCorrectEnabled parameter.

> Task :app:mergeExtDexDebug
> Task :app:compileDebugJavaWithJavac NO-SOURCE
> Task :app:bundleDebugClassesToCompileJar
> Task :app:dexBuilderDebug
> Task :app:mergeDebugGlobalSynthetics
> Task :app:processDebugJavaRes
> Task :app:compileDebugAndroidTestKotlin
> Task :app:compileDebugAndroidTestJavaWithJavac NO-SOURCE
> Task :app:mergeLibDexDebug
> Task :app:mergeProjectDexDebug
> Task :app:mergeDebugJniLibFolders
> Task :app:mergeDebugNativeLibs

> Task :app:stripDebugDebugSymbols
Unable to strip the following libraries, packaging them as they are: libandroidx.graphics.path.so.

> Task :app:mergeDebugJavaResource
> Task :app:validateSigningDebug
> Task :app:writeDebugAppMetadata
> Task :app:writeDebugSigningConfigVersions
> Task :app:mergeDebugAndroidTestShaders
> Task :app:compileDebugAndroidTestShaders NO-SOURCE
> Task :app:generateDebugAndroidTestAssets UP-TO-DATE
> Task :app:mergeDebugAndroidTestAssets
> Task :app:compressDebugAndroidTestAssets
> Task :app:desugarDebugAndroidTestFileDependencies
> Task :app:dexBuilderDebugAndroidTest
> Task :app:mergeDebugAndroidTestGlobalSynthetics
> Task :app:packageDebug
> Task :app:createDebugApkListingFileRedirect
> Task :app:processDebugAndroidTestJavaRes
> Task :app:mergeDebugAndroidTestJavaResource
> Task :app:checkDebugAndroidTestDuplicateClasses
> Task :app:mergeDebugAndroidTestJniLibFolders
> Task :app:mergeExtDexDebugAndroidTest
> Task :app:mergeLibDexDebugAndroidTest
> Task :app:mergeProjectDexDebugAndroidTest
> Task :app:mergeDebugAndroidTestNativeLibs NO-SOURCE
> Task :app:stripDebugAndroidTestDebugSymbols NO-SOURCE
> Task :app:validateSigningDebugAndroidTest
> Task :app:writeDebugAndroidTestSigningConfigVersions
> Task :app:packageDebugAndroidTest
> Task :app:createDebugAndroidTestApkListingFileRedirect
[EmulatorConsole]: Failed to start Emulator console for 5554

> Task :app:connectedDebugAndroidTest
Starting 6 tests on emulator-5554 - 11

com.buduping.app.HomeUiTest > narrowLargeText[emulator-5554 - 11] [31mFAILED [0m
	java.lang.AssertionError: Assert failed: The component is not displayed!
	at androidx.compose.ui.test.AssertionsKt.assertIsDisplayed(Assertions.kt:34)

emulator-5554 - 11 Tests 3/6 completed. (0 skipped) (1 failed)
Finished 6 tests on emulator-5554 - 11

> Task :app:connectedDebugAndroidTest
Tests on emulator-5554 - 11 failed: There was 1 failure(s).

> Task :app:connectedDebugAndroidTest FAILED

FAILURE: Build failed with an exception.

* What went wrong:
Execution failed for task ':app:connectedDebugAndroidTest'.
> There were failing tests. See the report at: file:///home/runner/work/BuduPing/BuduPing/android/app/build/reports/androidTests/connected/debug/index.html

* Try:
> Run with --stacktrace option to get the stack trace.
> Run with --info or --debug option to get more log output.
> Run with --scan to get full insights.
> Get more help at https://help.gradle.org.

BUILD FAILED in 4m 10s
65 actionable tasks: 65 executed
```
