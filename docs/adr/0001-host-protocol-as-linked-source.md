---
status: accepted
---

# The host protocol is source that both assemblies compile

The launcher and the host share one set of host protocol source files.
Both projects compile these files as linked items. There is no shared assembly.

The host runs with the deps file of the target app. A shared assembly is not in that file.
The host must then load it with a resolve hook before it can read its own options.
The launcher cannot reference `AxView.Host` either, because the host compiles against Avalonia 11.0 or 12.0 and the launcher uses 12.1.

## Consequences

- The protocol files must use no package references, because the host cannot ship package runtime assets.
- The protocol files must compile for each target framework of the host. Today that is `net8.0`.
- The test project references both assemblies. It uses an extern alias on the host reference to prevent duplicate type names.
