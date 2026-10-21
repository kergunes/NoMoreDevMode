# NoMoreDevMode / Earned Resurrection

V0.1 for RimWorld 1.7 + Ideology. Resurrection should not be random. It should be earned.

## Build

Requires the .NET 10 SDK and a local RimWorld 1.7 installation (Waylith).
The installed game's runtime is .NET 10; this project intentionally targets it.

```powershell
$env:RIMWORLD_DIR = 'your RimWorld installation directory'
dotnet build -c Release
```

Alternatively pass `-p:RimWorldDir="..."`, or create the ignored `Directory.Build.props.user`:

```xml
<Project><PropertyGroup><RimWorldDir>your game directory</RimWorldDir></PropertyGroup></Project>
```

The DLL is written to `Assemblies/`. Game assemblies are referenced locally and never distributed.
Implementation and validation are in progress; compilation alone is not gameplay acceptance.
