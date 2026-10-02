# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.

## Now


## Next

- [ ] **ViewModel picker (M).** Let the user select a ViewModel type for the current view.
  Remember the choice per file.
- [ ] **DI ViewModels (M).** Optional convention: a static factory or a `ServiceCollection` hook in the target app.
  Stub services so the preview makes no network calls.
  Test case: DAT4000 `ProgramEditorView` fails with "LoggerService not initialized".
- [ ] **Rebuild on .cs change (M).** Watch the project's `.cs` files.
  Run `dotnet build` on the app project, then start the host again on the same file.
  Keep the window position and size.
- [ ] **Preview size (S).** Presets (phone, tablet, desktop) and a custom width and height.
- [ ] **Watch included files (M).** Reload when a `ResourceInclude` or `StyleInclude` file of the project changes.
  An `avares://` include loads the compiled XAML of the built assembly, not the file on disk.
  So a watcher alone is not sufficient. The host must:
  - Watch all AXAML files of the project.
  - Find the include in the app styles and resources whose `Source` is the changed file.
  - Load the file from disk with the runtime loader. Replace the include with the result.
  - Render the view again. Keep the old include when the load fails.

## Later

- [ ] **Avalonia 12 (L).** Check the loader and host APIs. A second host build may be necessary.
- [ ] **Output choice (S).** When more than one output fits, prefer Debug. Show the choice in the status bar.
  Today the most recent build wins.
- [ ] **CI snapshot mode (S).** Render all files of a project. Write one PNG per file and a summary. Fail on error.
  Move the snapshot out of `PreviewWindow` into a snapshot runner that uses `PreviewRenderer`.
  Capture only the preview. Get the result from the renderer and the dispatcher errors, not from the error panel.
- [ ] **Packaging (M).** Publish as a `dotnet tool` (`axview`). Bundle the host in the tool package.
- [ ] **Linux and macOS check (S).** Run the tests and one snapshot on each OS.

## Known issues

- The preview window uses the target app's theme, so the AxView chrome looks different in each app.
- `TargetAppImporter` skips `OnFrameworkInitializationCompleted`.
  Data templates that need the app's services (for example a `ViewLocator` with DI) can fail.
- A style or resource preview shows the "No DataContext" badge. The badge is not applicable to these roots.

## Done

- [x] Launcher, host, hot reload, error overlay, snapshot mode, picker UI, project file list. `9ffd2dd`
- [x] Host protocol module (ADR 0001). `4eb967e`
- [x] Deep `PreviewRenderer`. `3d7e3f0`
- [x] XAML loader source behind an interface. `aa07d93`
- [x] Session supervisor: `HostSession` with a host process interface. `eebcde0`
- [x] Code-behind: the loader creates the `x:Class` type. Design mode when it has no parameterless constructor. `64016bc`
- [x] ViewModel from the XAML: `Design.DataContext`, else `x:DataType`, else a "No DataContext" badge. `20a92b3`
- [x] Picker window check: recent rows show the file name, then the folder. A hint replaces an empty list. `518bbcd`
- [x] Project switch recovery: a failed switch starts the host again on the previous file and shows the setup error. `b374865`
- [x] Design DataContext forms: `d:DataContext="{d:DesignInstance ...}"`, and a `Design.DataContext` with content. `37d9f7d`
- [x] Style and resource previews: `Design.PreviewWith` for a `Styles`, `Style`, or `ResourceDictionary` root. `efef44c`
- [x] Theme switch: a toolbar switch for Default, Light, and Dark (`RequestedThemeVariant`). `b385a86`
- [x] More DesignInstance forms: `Type={x:Type ...}`, and a `d:DataContext` with a `d:DesignInstance` on a child element. `086075a`
