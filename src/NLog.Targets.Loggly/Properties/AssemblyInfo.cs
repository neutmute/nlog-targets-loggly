using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NLog.Targets.Loggly")]
[assembly: AssemblyProduct("NLog.Targets.Loggly")]
[assembly: AssemblyCopyright("Copyright © Joe Fitzgerald 2012")]
[assembly: AssemblyDescription("A custom target for NLog that sends log messages to loggly.com using the loggly-csharp library")]
[assembly: AssemblyVersion("4.5.0.0")]

#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
    [assembly: AssemblyInformationalVersion("4.5.1-alpha-v4")]
#else
    [assembly: AssemblyConfiguration("Release")]
    [assembly: AssemblyInformationalVersion("4.5.0")]
#endif
