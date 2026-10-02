# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.
Each item has a number, for example `#16`. Give a new item the next free number.
Do not change or use again a number.

## Now


## Next

- [ ] #17 **DI ViewModels (M).** Optional convention: a static factory or a `ServiceCollection` hook in the target app.
  Stub services so the preview makes no network calls.
  Test case: DAT4000 `ProgramEditorView` fails with "LoggerService not initialized".
- [ ] #18 **Rebuild on .cs change (M).** Watch the project's `.cs` files.
  Run `dotnet build` on the app project, then start the host again on the same file.
  Keep the window position and size.
- [ ] #19 **Watch included files (M).** Reload when a `ResourceInclude` or `StyleInclude` file of the project changes.
  An `avares://` include loads the compiled XAML of the built assembly, not the file on disk.
  So a watcher alone is not sufficient. The host must:
  - Watch all AXAML files of the project.
  - Find the include in the app styles and resources whose `Source` is the changed file.
  - Load the file from disk with the runtime loader. Replace the include with the result.
  - Render the view again. Keep the old include when the load fails.

## Later

- [ ] #20 **Avalonia 12 (L).** Check the loader and host APIs. A second host build may be necessary.
- [ ] #21 **Output choice (S).** When more than one output fits, prefer Debug. Show the choice in the status bar.
  Today the most recent build wins.
- [ ] #22 **CI snapshot mode (S).** Render all files of a project. Write one PNG per file and a summary. Fail on error.
  Use `SnapshotWindow` for each file.
- [ ] #23 **Packaging (M).** Publish AxView as one portable exe (`axview.exe`).
  The host must be a `.dll` on disk, because `dotnet exec` starts it in the target runtime.
  Thus the exe must unpack the host before it starts the host.
  The settings file is next to the exe. It moves to `LocalApplicationData/AxView` only when the exe folder is not writable.
- [ ] #24 **Linux and macOS check (S).** Run the tests and one snapshot on each OS.
- [ ] #26 **ViewModel in snapshot mode (S).** A snapshot ignores the ViewModel that the user selected for the file.
  Use the choice from the settings file, or add a `--view-model <type>` option.
- [ ] #25 **About dialog (S).** Add a simple About dialog to the picker and the preview window.
  Show the AxView version, the build date, and the Avalonia version of the host.

## Known issues

- The preview window uses the target app's theme, so the AxView chrome looks different in each app.
- `TargetAppImporter` skips `OnFrameworkInitializationCompleted`.
  Data templates that need the app's services (for example a `ViewLocator` with DI) can fail.
- A style or resource preview shows the "No DataContext" badge. The badge is not applicable to these roots.

## Done

- [x] #1 Launcher, host, hot reload, error overlay, snapshot mode, picker UI, project file list. `9ffd2dd`
- [x] #2 Host protocol module (ADR 0001). `4eb967e`
- [x] #3 Deep `PreviewRenderer`. `3d7e3f0`
- [x] #4 XAML loader source behind an interface. `aa07d93`
- [x] #5 Session supervisor: `HostSession` with a host process interface. `eebcde0`
- [x] #6 Code-behind: the loader creates the `x:Class` type. Design mode when it has no parameterless constructor. `64016bc`
- [x] #7 ViewModel from the XAML: `Design.DataContext`, else `x:DataType`, else a "No DataContext" badge. `20a92b3`
- [x] #8 Picker window check: recent rows show the file name, then the folder. A hint replaces an empty list. `518bbcd`
- [x] #9 Project switch recovery: a failed switch starts the host again on the previous file and shows the setup error. `b374865`
- [x] #10 Design DataContext forms: `d:DataContext="{d:DesignInstance ...}"`, and a `Design.DataContext` with content. `37d9f7d`
- [x] #11 Style and resource previews: `Design.PreviewWith` for a `Styles`, `Style`, or `ResourceDictionary` root. `efef44c`
- [x] #12 Theme switch: a toolbar switch for Default, Light, and Dark (`RequestedThemeVariant`). `b385a86`
- [x] #13 More DesignInstance forms: `Type={x:Type ...}`, and a `d:DataContext` with a `d:DesignInstance` on a child element. `086075a`
- [x] #14 Settings file: `settings.json` next to the app keeps the recent files, the window placement, and the theme. `f3df631`
- [x] #15 Preview size: a toolbar switch for Fit, Phone, Tablet, Desktop, and a custom width and height. Kept in `settings.json`. `5f85977`
- [x] #26 Live preview module: `LivePreview` owns the file watch, the load, the last good preview, and the dispatcher errors. `549ba1e`
- [x] #27 Snapshot window: a snapshot captures only the preview. The exit reason comes from `LivePreview`, not from the error panel. `2230fcf`
- [x] #16 ViewModel picker: a toolbar switch selects a `*ViewModel` type. The settings file keeps the choice per file.
