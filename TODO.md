# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]`, add the commit hash, and move it to the top of Done.
Done has the newest item first.
Each item has a number, for example `#16`. Give a new item the next free number.
Do not change or use again a number.

## Now

## Next

- [ ] #82 **Stale app output (S).** `OutputFinder` prefers a Debug output to a newer Release output.
  The launcher does not build before the first host start.
  An old output gives errors such as `Unable to resolve "ShellDesignData.Login" as static field`.
  Do one or more of these steps:
  - Select the newest output, or show a warning when a Release output is newer than the Debug output.
  - Show a warning when a source file of the project is newer than the selected output.
  - Build the app before the first host start, when the output is old.
- [ ] #83 **ReactiveUI scheduler (S).** The host does not set the ReactiveUI main thread scheduler of the app.
  A `ReactiveCommand` that runs in the preview then raises `CanExecuteChanged` on the thread pool.
  A `Button` reads its `Command` on that thread, and the host stops with an unhandled exception.
  Error: `The calling thread cannot access this object because a different thread owns it.`
  The snapshot exit code was -532462766, not 1, and no report was written.
  Example: a design ViewModel that calls `AddCmd.Execute().Subscribe()` (DAT4000 `UserManagementDesignData`).
  Do these steps:
  - Set `RxSchedulers.MainThreadScheduler` to the Avalonia dispatcher when the app references ReactiveUI.
  - Catch an unhandled exception of a background thread. Show it in the error panel and in `--report`.
- [ ] #84 **Design mode flag (S).** The host does not set `Design.IsDesignMode`.
  App code that checks the flag runs its runtime path in the preview.
  Example: DAT4000 design data must test `LoggerService.IsInitialized` because the flag is false.
  Set the flag before the host loads the first file.
- [ ] #85 **Locked app output (M).** The host loads the assemblies from the app output folder.
  While a preview is open, a build of the app cannot replace these files (MSB3021).
  Load the assemblies from a shadow copy, or from memory, so that the app build can continue.

## Later

- [ ] #59 **Avalonia 11 host tests (M).** The test project tests only the Avalonia 12 host build (ADR 0002).
  Find a way to run the host tests also on the Avalonia 11 build, for example a second test run in CI.

## Done

- [x] #81 Launcher publish: `Private="false"` on the host references keeps their deps and runtimeconfig files out of the output. Before, `dotnet publish` failed with NETSDK1152. `PublishTests` publishes the launcher. `1ff1918`
- [x] #79 Snapshot report: `--report <file.json>` with `--snapshot` writes `SnapshotReport`: the exit reason, the load time, the error with its line and column, and the log. `LogCapture` catches the Avalonia warnings, for example binding errors. `2c7f770`
- [x] #78 Visual tree dump: `--tree <file.json>` with `--snapshot` writes the visual tree of the view with `VisualTreeDump`. Compact JSON, because indents made the file 7 times larger. `SnapshotRequest` holds the snapshot options of the launcher. `a2af3ce`
- [x] #80 Render flags: `--size` and `--theme` set the size and the theme variant of `--snapshot` and `--snapshot-all`. The gallery ignores the size. `PreviewSize.Parse` reads the size. `ac6ba58`
- [x] #77 Missing project folder: `PreviewWindowModel.RefreshCatalog` shows the error and keeps the old entries. Before, the dispatcher hook showed it as a file error. On Avalonia 11.0 the host stopped. `31831f1`
- [x] #76 Build window: the app icon, the app name, the time as `m:ss`, and the last line in monospace. It uses `ChromeStyle`. `3b684e9`
- [x] #75 Picker window: a header with the app icon and an About icon button, an accent Open button, file icons in the recent rows, and an error card. `06b8254`
- [x] #74 Status bar: the status, the output folder, and the DataContext pill as segments with separators. `f5e453d`
- [x] #73 Sidebar: a header with Open and collapse icon buttons, a search icon in the filter, `FileRow` with a kind icon and an accent selection bar. `IsSidebarCollapsed` in `settings.json`. `130a2ed`
- [x] #72 Error panel: `ErrorPanel` is a card with the error brush border, a header, copy and collapse buttons, and a last good render note. `1dfdca3`
- [x] #71 Preview stage: `PreviewStage` shows a size preset as a centered card with a shadow on the stage brush, and the size below it. Fit has no card and no backdrop. `0b2a2db`
- [x] #70 Preview toolbar: theme, size, and DataContext groups with an icon or a label, and separators. About is an icon button at the right edge. `ChromeControls` gives the icon button and the separator. `45601b1`
- [x] #69 Chrome icons: `ChromeIcons` gives a path geometry for each `ChromeIcon`, also a search icon for #73. The `icons` chrome shot shows all icons. `e25c012`
- [x] #68 Chrome style module: `ChromeStyle` gives a spacing scale, font sizes, a corner radius, and theme brushes per variant. The picker and the preview window use it. `290aea1`
- [x] #67 Chrome shots: `ChromeShotTests` write a PNG of each window in Fluent and Simple, Light and Dark. The headless tests draw with Skia. `b3c3ebb`
- [x] #66 Include update in one call: no change. `AppStyles` must then track each view search with a weak reference. That adds more code than the one check that it removes from `ProjectPreview`.
- [x] #65 Snapshot runner: `SnapshotRunner` runs `--snapshot` and `--snapshot-all` and gives the exit reason. The shutdown waits for the main loop. `9859a40`
- [x] #64 Window model: `PreviewWindowModel` owns the file list, the shown item, the status, and the settings. `ProjectPreview` gives `Error` and `StylesError`, and saves the hand-off. `9069225`
- [x] #63 Hand-off module: `HandOff` carries the exit reason. `Save` gives the exit code. `Take` reads and deletes the file in the launcher. `563f027`
- [x] #61 Gallery: each keyed `ControlTheme` of the app resources and app styles gets a row. Avalonia themes are ignored. `7ede774`
- [x] #62 Strict analyzers: all CA rules (`latest-all`) and all IDE rules are errors. `.editorconfig` tells the reason for each rule that is off. `66a48a7`
- [x] #60 Control gallery: a file list entry shows the common controls in each state and each app style class. `--snapshot-all` writes `axview-gallery.png`. `800b60d`
- [x] #58 README: what AxView does, the build, the options, the snapshot modes, and `settings.json`. `cb9462b`
- [x] #37 Own chrome theme: the launcher gets `Avalonia.Themes.Simple` from NuGet when the app ships no theme. `d200d03`
- [x] #57 Version test on a tag: the test project writes the MinVer version. The test compares the launcher version with it. `ec8bd65`
- [x] #56 Launcher on Avalonia 12: the picker and the tests use Avalonia 12.1.3 and xunit v3. The tests test the Avalonia 12 host build. `264b173`
- [x] #20 Avalonia 12: `AxView.Host12` compiles the host sources against Avalonia 12. The launcher selects `host` or `host12` (ADR 0002). `ce322b7`
- [x] #55 .NET 10: the launcher and the tests target `net10.0`. The host stays on `net8.0`. `2471184`
- [x] #39 Build progress: the launcher shows a build window with the app name, the time, and the last build line. Cancel stops the build. `f98134b`
- [x] #52 Includes in the children of a view: `ChangedIncludes` finds each include element of the file, also in child controls. `1c69607`
- [x] #51 StaticResource from an include of a view: `ChangedIncludes` gives the changed include files to `LoadGroup`. Then the load uses them. `1c69607`
- [x] #54 Pack only: `pack.ps1` publishes and writes the archives. It has no smoke check. `9e98ff6`
- [x] #53 Versions from git tags: MinVer gives the version from a `v<semver>` tag. `docs/RELEASE.md` has the tag rules. `14369a7`
- [x] #49 App.axaml data templates: `TargetStyles.ReplaceWith` replaces the data templates of each owner, and the theme variant. `ff65f2c`
- [x] #47 Packaging script: `pack.ps1 src|bin|all` writes the archives to `artifacts/`. A smoke check renders a new small app. `50bb0bf`
- [x] #50 Nested includes after an App.axaml reload: no change necessary. The reload gives the compiled content, not include objects.
- [x] #36 View includes: `AppIncludes` walks a control, `Styles`, `Style`, or `ResourceDictionary` root after each render. `8a00e2a`
- [x] #35 App.axaml reload: `AppStyles` loads a changed App.axaml into each live copy. A new copy gets the changed App.axaml and includes. `6e48d86`
- [x] #45 Project preview: `ProjectPreview` owns the catalog, the live preview, the include reload, the rebuild watch, and the ViewModels. It gives a stop request. `10f0e36`
- [x] #46 Host mode: `HostOptions.Mode`. The host and the launcher reject `--snapshot` with `--snapshot-all`, and `--view-model` without `--snapshot`. `45d8968`
- [x] #38 Include reload for the app resources of Application.Current: #44 does it. `f74593e`
- [x] #44 App styles: `AppStyles` gives a live copy to each owner. An include reload updates all live copies. `f74593e`
- [x] #43 Project set: one host module loads the assemblies of the view, app, and referenced projects. It replaces `ProjectRenderers`. `b687af9`
- [x] #25 About dialog: the picker and the preview window show the version, the build date, and the Avalonia version. `83b0fd3`
- [x] #42 Includes into another project: `AppIncludes` finds the project of an include by its avares URI. The host watches the AXAML files of each project. `6f08f06`
- [x] #41 Project reference watch: the launcher gives the ProjectReference folders in `--referenced-projects`. A `.cs` change in one starts a rebuild. `819b070`
- [x] #40 App resources in Application.Current: a `StaticResource` in a style root finds the resources of the app. `c976fd6`
- [x] #28 ViewModel in snapshot mode: `--view-model <full type name>` sets the DataContext of a `--snapshot`. An unknown type exits with 2. `db53797`
- [x] #22 CI snapshot mode: `--snapshot-all <dir>` writes one PNG per project file and `summary.txt`. The exit code is 1 when a file fails. `577d727`
- [x] #21 Output choice: a Debug output wins over a newer Release output. The status bar shows the output folder. `e8652ea`
- [x] #34 App startup services: `RunAppStartup` in `settings.json` calls `OnFrameworkInitializationCompleted` of the app. `AxViewDesign` docs name `App.Services`. `c58e640`
- [x] #33 Window styles on the frame: the frame has the style key of a window, and always an explicit theme. `eb3d514`
- [x] #32 Library include reload: `AppIncludes.Find` looks for App.axaml in the view project, then in the app project. `c928c7a`
- [x] #31 App project watch: the launcher gives the app project folder in `--app-project`. The host watches its C# files. `6419032`
- [x] #30 No DataContext badge on style roots: the renderer gives the root kind. The window hides the badge for a style root. `a7c2453`
- [x] #29 Own chrome styles: the app styles apply only in a scope around the preview. The chrome uses the Fluent or Simple theme. `e162858`
- [x] #19 Include reload: a changed `StyleInclude` or `ResourceInclude` file of the app loads from disk and replaces the compiled include. `d7ee20f`
- [x] #18 Rebuild: a change of a `.cs` file of the project stops the host. The launcher builds the app and starts the host again. `2faa127`
- [x] #17 Design services: the target app's `AxViewDesign.CreateServices()` gives the constructor parameters of the ViewModels. `13f2102`
- [x] #16 ViewModel picker: a toolbar switch selects a `*ViewModel` type. The settings file keeps the choice per file. `2a33eb3`
- [x] #27 Snapshot window: a snapshot captures only the preview. The exit reason comes from `LivePreview`, not from the error panel. `2230fcf`
- [x] #26 Live preview module: `LivePreview` owns the file watch, the load, the last good preview, and the dispatcher errors. `549ba1e`
- [x] #15 Preview size: a toolbar switch for Fit, Phone, Tablet, Desktop, and a custom width and height. Kept in `settings.json`. `5f85977`
- [x] #14 Settings file: `settings.json` next to the app keeps the recent files, the window placement, and the theme. `f3df631`
- [x] #13 More DesignInstance forms: `Type={x:Type ...}`, and a `d:DataContext` with a `d:DesignInstance` on a child element. `086075a`
- [x] #12 Theme switch: a toolbar switch for Default, Light, and Dark (`RequestedThemeVariant`). `b385a86`
- [x] #11 Style and resource previews: `Design.PreviewWith` for a `Styles`, `Style`, or `ResourceDictionary` root. `efef44c`
- [x] #10 Design DataContext forms: `d:DataContext="{d:DesignInstance ...}"`, and a `Design.DataContext` with content. `37d9f7d`
- [x] #9 Project switch recovery: a failed switch starts the host again on the previous file and shows the setup error. `b374865`
- [x] #8 Picker window check: recent rows show the file name, then the folder. A hint replaces an empty list. `518bbcd`
- [x] #7 ViewModel from the XAML: `Design.DataContext`, else `x:DataType`, else a "No DataContext" badge. `20a92b3`
- [x] #6 Code-behind: the loader creates the `x:Class` type. Design mode when it has no parameterless constructor. `64016bc`
- [x] #5 Session supervisor: `HostSession` with a host process interface. `eebcde0`
- [x] #4 XAML loader source behind an interface. `aa07d93`
- [x] #3 Deep `PreviewRenderer`. `3d7e3f0`
- [x] #2 Host protocol module (ADR 0001). `4eb967e`
- [x] #1 Launcher, host, hot reload, error overlay, snapshot mode, picker UI, project file list. `9ffd2dd`
