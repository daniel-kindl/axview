---
status: accepted
---

# One host build for each Avalonia major version

The host runs on the Avalonia assemblies of the target app.
Avalonia 12 breaks the binary API of Avalonia 11. For example, the indexer `AvaloniaObject[IndexerDescriptor]` takes a different binding type.
A host that compiles against Avalonia 11.0 then fails with `MissingMethodException` in an Avalonia 12 app.

The launcher ships two host builds from one set of sources:

- `AxView.Host` compiles against Avalonia 11.0. The launcher copies it to the `host` folder.
- `AxView.Host12` compiles against Avalonia 12.0. The launcher copies it to the `host12` folder.

`DotnetHostProcess` reads the Avalonia version from the deps file of the app. It selects the folder from the major version.

## Considered options

- One host build that binds each changed API by reflection. The compiler cannot find the binary breaks. Only a crash at run time shows them.
- `#if` blocks for each version. Two code paths in one file are hard to read and to test.

## Consequences

- The host sources must compile against both APIs. Use an API that both versions have.
- `AxView.Host12` ignores the obsolete warning (CS0618), because some Avalonia 11.0 APIs are obsolete in Avalonia 12.
- The test project tests one host build only. Today that is the Avalonia 11 build.
