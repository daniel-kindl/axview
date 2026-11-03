# Changelog

The notable changes to AxView are in this file.
The version numbers follow [semantic versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `--fit-content` crops a snapshot to the size that the view content needs.
- The preview runs in a copy of the app output. A build of the app can replace that output while the preview is open.
- `"SetDesignMode": true` in `settings.json` sets `Design.IsDesignMode`.

### Fixed

- A snapshot uses 96 DPI at each screen scale. On Avalonia 12, a screen scale above 1 scaled one region of the content a second time.
- The host reports an exception that a background thread throws.
- The host calls `UseReactiveUI` from `ReactiveUI.Avalonia.Reactive`.
- When a C# file is newer than the app output that AxView found, AxView builds the app before it starts the host.

### Security

- The Avalonia 11 host project pins SkiaSharp 2.88.9 and Tmds.DBus.Protocol 0.21.3.
- The Avalonia 12 host project pins Tmds.DBus.Protocol 0.94.1.
- The host does not ship those assemblies. The target app still supplies its own copies at runtime.

### Changed

- The Avalonia 12 host compiles against Avalonia 12.1.5.
- This repository follows dkstack v1.
- A task that is not done is a GitHub issue. `TODO.md` remains the historical list.

## [1.1.0] - 2026-10-06

### Added

- `--report` writes the snapshot result, the load time, the error, and the binding warnings.
- `--tree` writes the visual tree of a snapshot.
- `--size` and `--theme` set the snapshot size and the theme variant.
- The control gallery shows the common controls in each state, with the app styles and the keyed control themes.
- The preview window, the picker, the build window, and the error panel use one chrome style.
- `AxView.Host12` runs a preview for an app on Avalonia 12.
- The launcher and the tests target .NET 10 and Avalonia 12.
- A build window shows the app name, the time, and the last build line.

### Fixed

- AxView shows an error when the project folder is missing. The previous file list remains on screen.
- The launcher publish keeps the host runtimeconfig files out of the output.
- The launcher version matches the MinVer version.
- The chrome uses the Simple theme from NuGet when the app ships no theme.

## [1.0.0] - 2026-10-02

### Added

- AxView shows a live preview of one AXAML file.
- AxView loads that file again when you save it.
- The preview uses the Avalonia version and the build output of the target app.
- When a C# file changes, AxView builds the app.
- AxView then starts the preview again.
- A change to `App.axaml` or to an include file updates the preview.
- The picker keeps recent files.
- The preview window lists the AXAML files of the project.
- You can set the theme, the preview size, and the ViewModel.
- `--snapshot` and `--snapshot-all` write PNG files for scripts and CI.
- MinVer sets the version from a `v<semver>` git tag.

[Unreleased]: https://github.com/daniel-kindl/axview/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/daniel-kindl/axview/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/daniel-kindl/axview/releases/tag/v1.0.0
