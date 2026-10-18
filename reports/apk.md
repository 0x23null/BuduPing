# CI report: apk
- status: failure
- workflow: Build APK (BuduPing)
- run: 39147437353/2
- sha: ee8be3e13a03c519617fb602723ba2033832899f
- time: 2026-10-18T16:58:44Z

## build log (tail 300)
```
Downloading https://services.gradle.org/distributions/gradle-8.7-bin.zip
............10%.............20%.............30%.............40%............50%.............60%.............70%.............80%.............90%............100%
Exception in thread "main" java.lang.NoClassDefFoundError: org/gradle/internal/file/PathTraversalChecker
	at org.gradle.wrapper.Install.unzip(Install.java:327)
	at org.gradle.wrapper.Install.unzipLocal(Install.java:153)
	at org.gradle.wrapper.Install.fetchDistribution(Install.java:114)
	at org.gradle.wrapper.Install.access$400(Install.java:48)
	at org.gradle.wrapper.Install$1.call(Install.java:84)
	at org.gradle.wrapper.Install$1.call(Install.java:70)
	at org.gradle.internal.file.locking.ExclusiveFileAccessManager.access(ExclusiveFileAccessManager.java:69)
	at org.gradle.wrapper.Install.createDist(Install.java:70)
	at org.gradle.wrapper.WrapperExecutor.execute(WrapperExecutor.java:102)
	at org.gradle.wrapper.GradleWrapperMain.main(GradleWrapperMain.java:66)
Caused by: java.lang.ClassNotFoundException: org.gradle.internal.file.PathTraversalChecker
	at java.base/jdk.internal.loader.BuiltinClassLoader.loadClass(BuiltinClassLoader.java:641)
	at java.base/jdk.internal.loader.ClassLoaders$AppClassLoader.loadClass(ClassLoaders.java:188)
	at java.base/java.lang.ClassLoader.loadClass(ClassLoader.java:525)
	... 10 more
```
