# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.
Each item has a number, for example `#16`. Give a new item the next free number.
Do not change or use again a number.

## Now


## Next


## Later

- [ ] #35 **App.axaml reload (M).** Reload the app styles and resources when App.axaml changes.
  Load the disk text with `AvaloniaRuntimeXamlLoader.Load(xaml, assembly, rootInstance: new App())`.
  Take the styles as `TargetAppImporter` does. Put them in each live copy of `AppStyles`. Make `AppIncludes` again.
- [ ] #36 **Includes in a view (M).** Load a changed include of a view or of a `Style` root from disk.
  Make `AppIncludes` walk any loaded root and its XAML. Run it after each render.
- [ ] #37 **Own chrome theme (M).** When the app ships no Fluent or Simple theme, get `Avalonia.Themes.Simple` from NuGet.
  Use the Avalonia version of the app, as `NuGetLoaderSource` does for the loader.
  Add the package to the probe path of `LoaderResolver`. Then the chrome does not use a second instance of the app styles.
- [ ] #39 **Build progress (M).** Show the build progress during a rebuild.
  Option A: the launcher shows a "Building" window.
  Option B: the host runs from a copy of the output. It stays open during the build and shows a build overlay.
- [ ] #20 **Avalonia 12 (L).** Check the loader and host APIs. A second host build may be necessary.
- [ ] #24 **Linux and macOS check (S).** Run the tests and one snapshot on each OS.


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
