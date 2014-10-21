# nlog-targets-loggly #
NLog Target For Loggly. 

Install via [nuget](https://www.nuget.org/packages/NLog.Targets.Loggly/) with

	Install-Package nlog-targets-loggly

See the Demo project for a working sample. 
Be sure to create your own config file which is not included in the repo. Try something like
	
	C:\nlog-targets-loggly> copy .\src\Demo\example.loggly.user.config .\src\Demo\loggly.user.config

## Example Config ##
This NLog target project reads the loggly configuration as documented [here](https://github.com/karlseguin/loggly-csharp), so be sure to add the Loggly config section as well as NLog config similar to below

	<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  throwExceptions="true">
		<extensions>
		  <add assembly="NLog.Targets.Loggly" />
		</extensions>
		<variable name="DefaultLayout" value="${longdate} | ${level:uppercase=true:padding=5} | ${message} | ${exception:format=type,tostring}" />
		<variable name="AppName" value="Loggly NLog Target Demo" />
	
		<targets async="true">
		  <target name="ColorConsole" xsi:type="ColoredConsole" layout="${DefaultLayout}" />
		  <target name="Loggly" xsi:type="Loggly" layout="${message}" />
		</targets>
		<rules>
		  <logger name="*" minlevel="Info" writeTo="ColorConsole,Loggly" />
		</rules>
	</nlog>

