# ExtrabbitCode.Attributes

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?logo=windows)
![Inventor](https://img.shields.io/badge/Autodesk%20Inventor-2025%2B-0696D7)
[![UI](https://img.shields.io/badge/UI-WPF%20%2B%20ModernUi-0696D7)](https://github.com/TWiesendanger/ExtrabbitCode.Inventor.ModernUi)
![License](https://img.shields.io/badge/license-MIT-green)

An Autodesk Inventor add-in for reading and managing native Inventor attributes through a dockable WPF panel — no API code required.

**[Full documentation](https://attributes.extrabbitcode.com)** · **[Autodesk App Store](https://marketplace.autodesk.com/publisher-profile?id=200812101855337)**

---

## Installation

Download and install the add-in from the [Autodesk App Store](https://marketplace.autodesk.com/publisher-profile?id=200812101855337). Once installed, Inventor shows a new **ExtrabbitCode.Attributes** ribbon tab on next launch.

---

## Stack

| Layer        | Technology                  |
|--------------|-----------------------------|
| Runtime      | .NET 8 / WPF, x64           |
| Inventor API | `Autodesk.Inventor.Sdk` 2.0 |
| MVVM         | `CommunityToolkit.Mvvm` 8.4 |
| UI framework | [`ExtrabbitCode.Inventor.ModernUi`](https://github.com/TWiesendanger/ExtrabbitCode.Inventor.ModernUi) 1.0.2 |
| Logging      | `log4net` 3.3               |
| Telemetry    | `PostHog` 2.4               |

## Architecture

- **Standard Inventor add-in** wired up via `.addin` manifest and `StandardAddInServer`
- Dockable `UserInterfaceManager` panel backed by MVVM ViewModels
- Attribute tree built by walking `AttributeSets` on all owned objects of the active document

## Building

1. Clone and open in Visual Studio
2. Run the correct Inventor Version according to your installed version
3. Build — `BuildScript.cmd` copies outputs to the Inventor add-in folder automatically

## App Store bundle

For the Autodesk App Store the add-in ships as a registry-free [ApplicationPlugins bundle](https://www.autodesk.com/developer-network/app-store/inventor) (not the dev `BuildScript.cmd` deploy). Create it with:

```powershell
dotnet build ExtrabbitCode.Attributes\ExtrabbitCode.Attributes.csproj -c Release -p:Platform=x64 -p:CreateBundle=true -p:DeployToInventor=false
```

This produces, under `ExtrabbitCode.Attributes\bin\Bundle\`:

- `ExtrabbitCode.Attributes.bundle\` — `PackageContents.xml` + `Contents\` (the add-in, its dependencies, and a relative-path `.addin`)
- `ExtrabbitCode.Attributes.bundle.zip` — **upload this to the App Store**

To smoke-test before submitting, copy the `.bundle` folder into `%ProgramData%\Autodesk\ApplicationPlugins\` and start Inventor. The bundle manifests live in `ExtrabbitCode.Attributes\Addin\Bundle\`; keep the version in `PackageContents.xml` in sync with `AppVersion` in `Directory.Build.props`.

## Documentation site

The docs live in the `documentation/` folder — a [Fumadocs](https://fumadocs.vercel.app/) site built on React Router 7 and Tailwind CSS 4.

```bash
cd documentation
npm install
npm run dev
```