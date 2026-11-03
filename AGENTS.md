# Agent instructions

Before each task, read and follow [dkstack v1](https://raw.githubusercontent.com/daniel-kindl/dkstack/v1/AGENTS.md).
Start with its triage skill. dkstack governs the engineering process.

## Product

AxView renders one Avalonia AXAML file of any project.
It loads the file again when you save the file.

- `src/AxView` is the launcher. The command name is `axview`.
- `src/AxView.Host` is the host for Avalonia 11. The launcher starts it with `dotnet exec`.
- `src/AxView.Host12` compiles the same host sources for Avalonia 12.
- The launcher and the host both include the source files in `src/Protocol`. [ADR 0001](docs/adr/0001-host-protocol-as-linked-source.md) records that choice.
- `tests/AxView.Tests` tests the launcher and the Avalonia 12 host.

Read [GLOSSARY.md](GLOSSARY.md) for the project terms.
Read [docs/agents/CODING.md](docs/agents/CODING.md) before you change C# code.
The build checks the rules that an analyzer can check.

## Work

A task that is not done is a GitHub issue in `daniel-kindl/axview`.
`TODO.md` is the historical work list. Do not add an item there.
A number in an old commit, for example `#86`, is a historical item number.
That number is not a GitHub issue number.

## Checks

Finish the task only when all of these conditions are true.

- The code does the task.
- `dotnet test` passes.
- `dotnet format --verify-no-changes --severity info` reports no changes.

For a host change, render one real view.

`dotnet src/AxView/bin/Debug/net10.0/axview.dll <file.axaml> --snapshot <file.png>`

Look at the PNG. A non-zero exit code means that the render failed.

For a chrome change, run `dotnet test --filter ChromeShot`.
Look at the PNGs in `tests/AxView.Tests/bin/Debug/net10.0/chrome-shots`.

## Process

Use [conventional commits and short-lived branches](https://raw.githubusercontent.com/daniel-kindl/dkstack/v1/version-control/index.md).
The default branch is `main`.
Use [semantic versioning](https://raw.githubusercontent.com/daniel-kindl/dkstack/v1/versioning/index.md).
MinVer reads the version from the git tags. [docs/RELEASE.md](docs/RELEASE.md) has the tag rules.
[CHANGELOG.md](CHANGELOG.md) lists the notable changes.
