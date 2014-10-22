using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NLog.Targets.Loggly")]
[assembly: AssemblyProduct("NLog.Targets.Loggly")]
[assembly: AssemblyCopyright("Copyright © Joe Fitzgerald 2012")]
[assembly: AssemblyDescription("A custom target for NLog allowing you to send log messages to loggly.com using the loggly-csharp library")]
[assembly: AssemblyVersion("4.5.0.0")]
[assembly: AssemblyInformationalVersion("4.5.0-alpha")]

#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration("Release")]
#endif
