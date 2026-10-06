# AxView

AxView shows a live preview of one Avalonia AXAML file of any project.
It renders the file on the Avalonia version of the target app.

## Processes

**Launcher**:
The `axview` program that the user starts. It finds the target app and starts the host.
_Avoid_: CLI, client

**Host**:
The process that shows the preview. It runs inside the runtime of the target app.
The launcher ships one host build for each Avalonia major version: `host` for Avalonia 11, `host12` for Avalonia 12.
_Avoid_: previewer, server, child

**Session**:
One run of the launcher for the user. A session can start more than one host, one after the other.
_Avoid_: run, loop

**Host protocol**:
The contract between the launcher and the host: the host options, the exit reasons, and the hand-off.
_Avoid_: arguments, command line

**Exit reason**:
The cause that a host or a launcher gives when it stops, as a named exit code.
_Avoid_: exit code, status, return value

**Hand-off**:
The data that a stopped host gives to the launcher for the next host, for example the next file.
_Avoid_: switch file, message

## Target

**Target app**:
The executable Avalonia app whose runtime and Avalonia version the host uses.
_Avoid_: user app, client app

**App output**:
One build folder of the target app that has the runtime and dependency files that the host needs.
_Avoid_: bin folder, build

**XAML loader**:
The Avalonia runtime XAML loader package that agrees with the Avalonia version of the target app.
_Avoid_: loader assembly, compiler

**Chrome theme**:
The Fluent or Simple theme of the AxView controls around the preview. The launcher gets the Simple theme from NuGet when the target app ships neither.
_Avoid_: host theme, AxView styles

**Preview target**:
All the facts that the launcher finds for one AXAML file before it starts a host.
_Avoid_: target, config

**View project**:
The project that contains the shown AXAML file. It can be the app project or a library.
_Avoid_: local project, current project

**App project**:
The project of the target app. For a library view, it is a different project from the view project.
_Avoid_: exe project, startup project

**Referenced project**:
A project that the view project or the app project references. Its code and include files belong to the preview too.
_Avoid_: dependency, library project

**Project set**:
The view project, the app project, and the referenced projects of one preview, each with its assembly.
_Avoid_: solution, project list

**App styles**:
The styles, resources, data templates, and theme variant that App.axaml of the target app loads. Each place that shows them gets its own copy.
_Avoid_: target styles, theme

## Preview

**Preview**:
The rendered result of one AXAML file that the host shows.
_Avoid_: view, render, page

**Live preview**:
The host module that shows one file, loads it again on each save, and keeps the last good preview on an error.
_Avoid_: reloader, preview session

**Project preview**:
The host module that shows the files of one project. It decides when the host stops, and which file the next host shows.
_Avoid_: preview session, controller, workspace

**Stop request**:
The exit reason and the next file that a project preview gives when the host must stop.
_Avoid_: shutdown, quit

**Host mode**:
The kind of work that one host does: an interactive preview, one snapshot, or a snapshot of each file of the project.
_Avoid_: run mode, flags

**Project catalog**:
The list of the AXAML files of one project that the host can show without a restart.
_Avoid_: file list, index

**Last good preview**:
The most recent preview that loaded without an error. The host shows it again when a new load fails.
_Avoid_: fallback, cached render

**Project switch**:
The change to an AXAML file of a different project. It needs a new host.
_Avoid_: reopen, reload

**Rebuild**:
The build of the target app after a code file of the project changes. A new host then shows the same file.
_Avoid_: recompile, restart

**Include reload**:
The load of a changed `StyleInclude` or `ResourceInclude` file of the app from disk. The result replaces the compiled include.
_Avoid_: style reload, hot reload

**Design services**:
The service provider that the target app gives through `AxViewDesign.CreateServices()`. The host creates the ViewModels with it.
_Avoid_: DI container, mocks, stubs

**Snapshot**:
A PNG image of a preview, written without user interaction.
_Avoid_: screenshot, capture
