using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NLog.Targets.Loggly")]
[assembly: AssemblyDescription("NLog target for Loggly")]

#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyProduct("NLog.Targets.Loggly")]
[assembly: AssemblyCopyright("Copyright © Joe Fitzgerald 2012")]

[assembly: AssemblyVersion("4.5.0.0")]
