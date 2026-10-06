# AxView

AxView shows a live preview of one Avalonia AXAML file of any project.
When you save the file, AxView loads it again.

- The preview runs on the runtime and the Avalonia version of your app (Avalonia 11 or 12).
- A change of a C# file of the project starts a build of the app. Then the preview starts again.
- A change of App.axaml or of a `StyleInclude` or `ResourceInclude` file updates the preview.
- An error in the file shows an error panel. The last good preview stays.
- The "Control gallery" entry of the file list shows the common controls in each state, with the app styles.
- A snapshot mode writes PNG files for scripts and CI.

## Requirements

- The .NET 10 SDK to build and run the launcher.
- An Avalonia 11 or 12 app on .NET 8 or later.
- A build of the app. AxView uses the app output in the `bin` folder of the app project.
- Access to NuGet, or the packages in the NuGet cache. See [Packages from NuGet](#packages-from-nuget).

## Build

```sh
dotnet build
dotnet test
```

The launcher is `src/AxView/bin/Debug/net10.0/axview.dll`.
The build copies the two host builds into the `host` and `host12` folders next to it.

To write the source and binary archives to `artifacts/`, run `./pack.ps1 all` (PowerShell 7).

## Use

```text
axview [<file.axaml> [--output <app-bin-dir>] [--snapshot <file.png> [--view-model <type>] | --snapshot-all <dir>]
       [--size <Fit|Phone|Tablet|Desktop|WxH>] [--theme <Default|Light|Dark>]]
```

| Command | Result |
|---|---|
| `axview` | Opens the picker window with the recent files. |
| `axview Views/MainView.axaml` | Opens the preview window for the file. |
| `axview Views/MainView.axaml --snapshot main.png` | Writes one PNG and exits. |
| `axview Views/MainView.axaml --snapshot-all shots` | Writes one PNG for each AXAML file of the project and exits. |
| `axview Views/MainView.axaml --snapshot main.png --size Phone --theme Dark` | Writes one PNG at the phone size in the dark variant. |

### Options

| Option | Meaning |
|---|---|
| `--output <app-bin-dir>` | Use this app output. Without it, AxView finds the output: a Debug output wins, then the most recent build. |
| `--snapshot <file.png>` | Write a PNG of the file, then exit. |
| `--view-model <type>` | Set the DataContext of a `--snapshot` to a new instance of this ViewModel type (full name). |
| `--snapshot-all <dir>` | Write a PNG of each AXAML file of the project and `summary.txt` into the folder, then exit. |
| `--size <size>` | Set the size of each snapshot: `Fit`, `Phone` (390×844), `Tablet` (820×1180), `Desktop` (1920×1080), or a width and height, for example `500x300`. `Fit` is 1280×800. |
| `--theme <variant>` | Set the theme variant of each snapshot: `Default` (the variant of the app), `Light`, or `Dark`. |
| `-h`, `--help` | Show the usage. |

Use `--snapshot` or `--snapshot-all`, not both.
`--size` and `--theme` need `--snapshot` or `--snapshot-all`. The names can have any case.
A snapshot does not use the size and the theme of `settings.json`.

### Preview window

- The list at the left shows the AXAML files of the project. A click shows the file in the same window.
- **Open...** opens a file of any project. A file of another project starts the host again.
- The toolbar selects the theme variant (Default, Light, Dark), the preview size (Fit, Phone, Tablet, Desktop, Custom), and the DataContext.
- The status bar shows the render time, the file, and the app output.

### DataContext of a preview

AxView sets the DataContext from the first of these that the file has:

1. `Design.DataContext` or `d:DataContext` (also `d:DesignInstance`).
2. `x:DataType`. AxView creates an instance of the type.

You can also select a `*ViewModel` type in the toolbar. The settings file keeps the selection for each file.

A ViewModel that needs constructor parameters gets them from design services.
Add this class to the project. It needs no reference to AxView:

```csharp
public static class AxViewDesign
{
    public static IServiceProvider CreateServices() { ... }
}
```

The factory must also set the static services that the app reads, for example `App.Services`.
Register stub services, so that the preview makes no network calls.

### Snapshot modes

`--snapshot` writes one PNG.
`--snapshot-all` writes the PNG of `Views/Main.axaml` to `Views/Main.png` in the folder.
It also writes `summary.txt`, with one tab-separated line for each file:

```text
ok      Views/Main.axaml
FAIL    Views/Broken.axaml    The error text, on one line
```

A failed file does not stop the run.

`--snapshot-all` also writes the control gallery to `axview-gallery.png`.
The gallery has its full size. It ignores `--size`.
The last line of `summary.txt` is for the gallery: `ok      (control gallery)`.

### Control gallery

The gallery shows the common controls with the styles of the app.
Each section has one row for each variant and one column for each state.

- The states are normal, pointer over, pressed, focus, disabled, and error.
- The gallery sets the pseudo-classes, for example `:pointerover`. Only the normal column takes input.
- Each style class of the app on a gallery control gets a row, for example `Button .primary`.
- Each control theme with a key in the app resources gets a row, for example `Button {PillButton}`.
- A control that fails in layout shows its error in its cell. The other cells stay.

The gallery cannot show a state that needs a real popup or real focus, for example an open ComboBox or a focus adorner.

### Exit codes

| Code | Meaning |
|---|---|
| 0 | The preview or the snapshot is good. |
| 1 | The snapshot shows a render error. In `--snapshot-all`, one file or more failed. |
| 2 | The options are not valid. |
| 4 | AxView cannot prepare the preview, for example because the app is not built. |

## Settings

AxView keeps its settings in `settings.json` next to `axview.dll`.
When that folder is not writable, the file is in the local application data folder, in `AxView`.
Each value is optional:

```json
{
  "RecentFiles": [ "/src/Ui/Views/Main.axaml" ],
  "Window": { "Mode": "Maximized", "X": 10, "Y": 20, "Width": 1280, "Height": 800 },
  "Theme": "Dark",
  "Size": { "Preset": "Custom", "Width": 500, "Height": 300 },
  "ViewModels": { "/src/Ui/Views/Main.axaml": "Ui.ViewModels.MainViewModel" },
  "IsSidebarCollapsed": true,
  "RunAppStartup": true
}
```

| Key | Meaning |
|---|---|
| `RecentFiles` | The files that the picker shows, newest first. |
| `Window` | The state and the bounds of the preview window. |
| `Theme` | The theme variant: `Default`, `Light`, or `Dark`. |
| `Size` | The preview size: `Fit`, `Phone`, `Tablet`, `Desktop`, or `Custom` with a width and a height. |
| `ViewModels` | The ViewModel type that you selected for each file. |
| `IsSidebarCollapsed` | `true` when you collapsed the sidebar of the preview window. |
| `RunAppStartup` | `true` calls `OnFrameworkInitializationCompleted` of the app. Its `ApplicationLifetime` is null. The default is `false`. You set this value in the file. |

AxView writes all values except `RunAppStartup`.

## Packages from NuGet

AxView gets these packages in the Avalonia version of the app:

- `Avalonia.Markup.Xaml.Loader`. Apps do not usually ship the runtime XAML loader.
- `Avalonia.Themes.Simple`, when the app ships no Fluent or Simple theme. The AxView controls use it.

AxView takes the packages from the NuGet cache (`NUGET_PACKAGES`, or `~/.nuget/packages`).
When a package is missing, AxView restores it with `dotnet restore`.

## Versions and releases

[docs/RELEASE.md](docs/RELEASE.md) tells how AxView gets its version from git tags, and how to make a release.

## More documents

- [GLOSSARY.md](GLOSSARY.md): the words of the project.
- [docs/adr](docs/adr): the architecture decisions.
- [AGENTS.md](AGENTS.md): the rules for agents that change the code.
