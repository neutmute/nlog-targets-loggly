msbuild ..\NLog.Targets.Loggly.sln /p:configuration=debug  /t:clean,build
msbuild ..\NLog.Targets.Loggly.sln /p:configuration=release /t:clean,build