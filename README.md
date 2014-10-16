# nlog-targets-loggly #
NLog Target For Loggly. 

See Demo project for a working sample. 
Be sure to update with your own ApiKey.


## Example Config ##

	<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  throwExceptions="true">
	<extensions>
	  <add assembly="NLog.Targets.Loggly" />
	</extensions>
	<variable name="DefaultLayout" value="${longdate} | ${level:uppercase=true:padding=5} | ${message} | ${exception:format=type,tostring}" />
	<variable name="AppName" value="Loggly NLog Target Demo" />
	
	<targets async="true">
	  <target name="ColorConsole" xsi:type="ColoredConsole" layout="${DefaultLayout}" />
	  <target name="Loggly" xsi:type="Loggly" layout="${message}" ApplicationName="${AppName}"  InputKey="YourInputKeyHere" />
	</targets>
	<rules>
	  <logger name="*" minlevel="Info" writeTo="ColorConsole,Loggly" />
	</rules>
	</nlog> 