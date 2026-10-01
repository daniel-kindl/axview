# TODO

Work list for agents. Do the top open item first.
Effort: S = one session, M = two or three sessions, L = more.
When you finish an item, mark it `[x]` and add the commit hash.

## Now

- [x] **Host protocol (S).** Put the host options, the exit reasons, and the hand-off in one module. See ADR 0001.
  Both assemblies compile its source. Decode the options strictly. Test a round trip. `4eb967e`
- [x] **Deepen the preview renderer (M).** `PreviewRenderer` takes a path and returns the preview control.
  It owns the read retry, the base URI, the namespace index, and the root mapping.
  Test it with Avalonia.Headless in the test project only.
  Add notes to the result when the "no DataContext" badge needs them.
- [ ] **Testable preview target (S).** Put the XAML loader source behind an interface (NuGet and a fake).
  Test `Resolve` on temporary folders.
- [ ] **Code-behind (S).** Find the `x:Class` type in the built assembly.
  Create it and fill it from the new XAML (`rootInstance`), so code-behind runs.
  Fall back to design mode when the type has no parameterless constructor.
- [ ] **ViewModel from the XAML (S).** Apply `Design.DataContext` (`Design.ApplyDesignModeProperties` by reflection).
  Else, create the `x:DataType` type if it has a parameterless constructor.
  Else, show a "no DataContext" badge in the status bar.
- [ ] **Check the picker window (S).** Nobody has looked at its layout yet.
  Test the project switch from the preview "Open..." button by hand.

## Next

- [ ] **Session supervisor (M).** Do this before "Rebuild on .cs change".
  Give `HostSession` a host process interface (real and fake) and the preview target resolver.
  Test the project switch, a missing hand-off, and a setup error.
- [ ] **Project switch recovery (S).** When the next file fails to resolve, start the host again on the previous file.
  Show the setup error in the error overlay.
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
