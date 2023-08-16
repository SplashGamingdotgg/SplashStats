using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("SplashStats")]
[assembly: AssemblyDescription("Harmony based stats tracking for Rust, with RCON support.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Splash Gaming, LLC")]
[assembly: AssemblyProduct("SplashStats")]
[assembly: AssemblyCopyright("Copyright © 2023 Splash Gaming, LLC. All rights reserved.")]
[assembly: AssemblyTrademark("Splash")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("3C1E386B-5F11-44CB-AED8-EDB000821F9B")]

// Defaults are: major.minor.build.rev
// Ours are: major.minor.commit# (automatically calculated with CI)

// Only increment this when backwards compatibility is broken
[assembly: AssemblyVersion("1.0")]

// File dialog/bin version
[assembly: AssemblyFileVersion("1.0.1")]

// Same as file version but with short commit hash attached.
[assembly: AssemblyInformationalVersion("1.0.1-0")]
