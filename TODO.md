# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.
Each item has a number, for example `#16`. Give a new item the next free number.
Do not change or use again a number.

## Now

Items #63 to #66 come from the architecture review of 2026-10-06.

- [ ] #63 **Hand-off module (S).** `HandOff` carries the exit reason and the next file.
  The host saves it and gets the exit code. The launcher takes it with the exit code.
  `StopRequest` goes. `HostSession` does not decode the exit codes.
- [ ] #64 **Window model (M).** A headless `PreviewWindowModel` owns the state of the preview window:
  - the file list with the filter, the gallery entry, and the selection
  - the shown item: a file or the control gallery
  - the status: the error, the status line, the title, and the DataContext badge
  - the settings file and the stop with a hand-off

  `PreviewWindow` only binds its controls to the model. Tests for each rule run without a window.
  `ProjectPreview` gives the include error of the last load in `Error`. Its deferred `Failed` event for this error goes.
  While the gallery shows, an include change rebuilds the gallery, and the status names the gallery.
- [ ] #65 **Snapshot runner (S).** One module runs `--snapshot` and `--snapshot-all`, and gives the exit reason.
  It finds the ViewModel, waits for the settle time, and writes the PNG files.
  `PreviewApp` only selects the mode. An unknown ViewModel gets a headless test.
- [ ] #66 **Include update in one call (S).** `AppStyles.Update` also tells if an include of the shown view changed.
  `ProjectPreview` makes one call and does not know the order of the checks.

## Next

## Later

- [ ] #59 **Avalonia 11 host tests (M).** The test project tests only the Avalonia 12 host build (ADR 0002).
  Find a way to run the host tests also on the Avalonia 11 build, for example a second test run in CI.


## Known issues

Each known issue is now an item: #30 to #39.

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
- [x] #30 No DataContext badge on style roots: the renderer gives the root kind. The window hides the badge for a style root. `a7c2453`
- [x] #31 App project watch: the launcher gives the app project folder in `--app-project`. The host watches its C# files. `6419032`
- [x] #32 Library include reload: `AppIncludes.Find` looks for App.axaml in the view project, then in the app project. `c928c7a`
- [x] #33 Window styles on the frame: the frame has the style key of a window, and always an explicit theme. `eb3d514`
- [x] #34 App startup services: `RunAppStartup` in `settings.json` calls `OnFrameworkInitializationCompleted` of the app. `AxViewDesign` docs name `App.Services`. `c58e640`
- [x] #21 Output choice: a Debug output wins over a newer Release output. The status bar shows the output folder. `e8652ea`
- [x] #22 CI snapshot mode: `--snapshot-all <dir>` writes one PNG per project file and `summary.txt`. The exit code is 1 when a file fails. `577d727`
- [x] #28 ViewModel in snapshot mode: `--view-model <full type name>` sets the DataContext of a `--snapshot`. An unknown type exits with 2. `db53797`
- [x] #40 App resources in Application.Current: a `StaticResource` in a style root finds the resources of the app. `c976fd6`
- [x] #41 Project reference watch: the launcher gives the ProjectReference folders in `--referenced-projects`. A `.cs` change in one starts a rebuild. `819b070`
- [x] #42 Includes into another project: `AppIncludes` finds the project of an include by its avares URI. The host watches the AXAML files of each project. `6f08f06`
- [x] #25 About dialog: the picker and the preview window show the version, the build date, and the Avalonia version. `83b0fd3`
- [x] #43 Project set: one host module loads the assemblies of the view, app, and referenced projects. It replaces `ProjectRenderers`. `b687af9`
- [x] #44 App styles: `AppStyles` gives a live copy to each owner. An include reload updates all live copies. `f74593e`
- [x] #38 Include reload for the app resources of Application.Current: #44 does it. `f74593e`
- [x] #46 Host mode: `HostOptions.Mode`. The host and the launcher reject `--snapshot` with `--snapshot-all`, and `--view-model` without `--snapshot`. `45d8968`
- [x] #45 Project preview: `ProjectPreview` owns the catalog, the live preview, the include reload, the rebuild watch, and the ViewModels. It gives a stop request. `10f0e36`
- [x] #35 App.axaml reload: `AppStyles` loads a changed App.axaml into each live copy. A new copy gets the changed App.axaml and includes. `6e48d86`
- [x] #36 View includes: `AppIncludes` walks a control, `Styles`, `Style`, or `ResourceDictionary` root after each render. `8a00e2a`
- [x] #50 Nested includes after an App.axaml reload: no change necessary. The reload gives the compiled content, not include objects.
- [x] #47 Packaging script: `pack.ps1 src|bin|all` writes the archives to `artifacts/`. A smoke check renders a new small app. `50bb0bf`
- [x] #49 App.axaml data templates: `TargetStyles.ReplaceWith` replaces the data templates of each owner, and the theme variant. `ff65f2c`
- [x] #53 Versions from git tags: MinVer gives the version from a `v<semver>` tag. `docs/RELEASE.md` has the tag rules. `14369a7`
- [x] #54 Pack only: `pack.ps1` publishes and writes the archives. It has no smoke check. `9e98ff6`
- [x] #51 StaticResource from an include of a view: `ChangedIncludes` gives the changed include files to `LoadGroup`. Then the load uses them. `1c69607`
- [x] #52 Includes in the children of a view: `ChangedIncludes` finds each include element of the file, also in child controls. `1c69607`
- [x] #39 Build progress: the launcher shows a build window with the app name, the time, and the last build line. Cancel stops the build. `f98134b`
- [x] #55 .NET 10: the launcher and the tests target `net10.0`. The host stays on `net8.0`. `2471184`
- [x] #20 Avalonia 12: `AxView.Host12` compiles the host sources against Avalonia 12. The launcher selects `host` or `host12` (ADR 0002). `ce322b7`
- [x] #56 Launcher on Avalonia 12: the picker and the tests use Avalonia 12.1.3 and xunit v3. The tests test the Avalonia 12 host build. `264b173`
- [x] #57 Version test on a tag: the test project writes the MinVer version. The test compares the launcher version with it. `ec8bd65`
- [x] #37 Own chrome theme: the launcher gets `Avalonia.Themes.Simple` from NuGet when the app ships no theme. `d200d03`
- [x] #58 README: what AxView does, the build, the options, the snapshot modes, and `settings.json`. `cb9462b`
- [x] #60 Control gallery: a file list entry shows the common controls in each state and each app style class. `--snapshot-all` writes `axview-gallery.png`. `800b60d`
- [x] #62 Strict analyzers: all CA rules (`latest-all`) and all IDE rules are errors. `.editorconfig` tells the reason for each rule that is off. `66a48a7`
- [x] #61 Gallery: each keyed `ControlTheme` of the app resources and app styles gets a row. Avalonia themes are ignored. `7ede774`
