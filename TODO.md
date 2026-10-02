# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.
Each item has a number, for example `#16`. Give a new item the next free number.
Do not change or use again a number.

## Now


## Next


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
- [ ] #28 **ViewModel in snapshot mode (S).** A snapshot ignores the ViewModel that the user selected for the file.
  Use the choice from the settings file, or add a `--view-model <type>` option.
- [ ] #25 **About dialog (S).** Add a simple About dialog to the picker and the preview window.
  Show the AxView version, the build date, and the Avalonia version of the host.

## Known issues

- The preview frame takes only the setters of the target `Window` control theme. A `Style Selector="Window"` of the app does not apply to the preview.
- Code that reads the app resources through `Application.Current` does not find them. They are in the preview scope.
- When the app ships no Fluent or Simple theme, the AxView chrome uses a second instance of the app styles.
- `TargetAppImporter` skips `OnFrameworkInitializationCompleted`.
  Data templates that need the app's services (for example a `ViewLocator` with DI) can fail.
- A style or resource preview shows the "No DataContext" badge. The badge is not applicable to these roots.
- A rebuild watches only the `.cs` files of the project of the file. A change in the app project of a library view starts no rebuild.
- During a rebuild, no window is open. The user sees no build progress.
- An include file reloads only when App.axaml is in the project of the file. A library view gets no include reload.
- A change of App.axaml itself does not reload. An include in a view or in a `Style` root loads the compiled file.

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
- [x] #16 ViewModel picker: a toolbar switch selects a `*ViewModel` type. The settings file keeps the choice per file. `2a33eb3`
- [x] #17 Design services: the target app's `AxViewDesign.CreateServices()` gives the constructor parameters of the ViewModels. `13f2102`
- [x] #18 Rebuild: a change of a `.cs` file of the project stops the host. The launcher builds the app and starts the host again. `2faa127`
- [x] #19 Include reload: a changed `StyleInclude` or `ResourceInclude` file of the app loads from disk and replaces the compiled include. `d7ee20f`
- [x] #29 Own chrome styles: the app styles apply only in a scope around the preview. The chrome uses the Fluent or Simple theme. `e162858`
