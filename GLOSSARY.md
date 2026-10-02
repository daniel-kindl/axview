# AxView

AxView shows a live preview of one Avalonia AXAML file of any project.
It renders the file on the Avalonia version of the target app.

## Processes

**Launcher**:
The `axview` program that the user starts. It finds the target app and starts the host.
_Avoid_: CLI, client

**Host**:
The process that shows the preview. It runs inside the runtime of the target app.
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

**Preview target**:
All the facts that the launcher finds for one AXAML file before it starts a host.
_Avoid_: target, config

## Preview

**Preview**:
The rendered result of one AXAML file that the host shows.
_Avoid_: view, render, page

**Live preview**:
The host module that shows one file, loads it again on each save, and keeps the last good preview on an error.
_Avoid_: reloader, preview session

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

**Design services**:
The service provider that the target app gives through `AxViewDesign.CreateServices()`. The host creates the ViewModels with it.
_Avoid_: DI container, mocks, stubs

**Snapshot**:
A PNG image of a preview, written without user interaction.
_Avoid_: screenshot, capture
