# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.

## Now


## Next

- [ ] **Project switch recovery (S).** When the next file fails to resolve, start the host again on the previous file.
  Show the setup error in the error overlay.
- [ ] **Design DataContext forms (S).** Support `d:DataContext="{d:DesignInstance ...}"` and a `Design.DataContext` with content.
  Today only `<Design.DataContext><vm:Type /></Design.DataContext>` is removed when its type is not creatable.
- [ ] **ViewModel picker (M).** Let the user select a ViewModel type for the current view.
  Remember the choice per file.
- [ ] **DI ViewModels (M).** Optional convention: a static factory or a `ServiceCollection` hook in the target app.
  Stub services so the preview makes no network calls.
- [ ] **Rebuild on .cs change (M).** Watch the project's `.cs` files.
  Run `dotnet build` on the app project, then start the host again on the same file.
  Keep the window position and size.
- [ ] **Style and resource previews (S).** Show `Design.PreviewWith` for a `Styles` or `ResourceDictionary` root.
- [ ] **Theme switch (S).** Toolbar toggle for Light, Dark, and Default (`RequestedThemeVariant`).
- [ ] **Preview size (S).** Presets (phone, tablet, desktop) and a custom width and height.
- [ ] **Watch included files (S).** Reload when a `ResourceInclude` or `StyleInclude` file of the project changes.

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

## Done

- [x] Launcher, host, hot reload, error overlay, snapshot mode, picker UI, project file list. `9ffd2dd`
- [x] Host protocol module (ADR 0001). `4eb967e`
- [x] Deep `PreviewRenderer`. `3d7e3f0`
- [x] XAML loader source behind an interface. `aa07d93`
- [x] Session supervisor: `HostSession` with a host process interface. `eebcde0`
- [x] Code-behind: the loader creates the `x:Class` type. Design mode when it has no parameterless constructor. `64016bc`
- [x] ViewModel from the XAML: `Design.DataContext`, else `x:DataType`, else a "No DataContext" badge. `20a92b3`
- [x] Picker window check: recent rows show the file name, then the folder. A hint replaces an empty list. `518bbcd`
