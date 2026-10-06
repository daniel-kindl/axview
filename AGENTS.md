# AGENTS.md

Rules for agents that write or change code in AxView.
If a rule does not answer your question, ask the user.

## What AxView is

AxView renders one Avalonia AXAML file of any project, and reloads it when the file changes.

- `src/AxView`: the launcher (`axview`). With no arguments, it shows a picker window with recent files.
  It finds the project, the built app output, and the matching XAML loader.
  It starts the host again when the user opens a file of another project.
- `src/AxView.Host`: the host. The launcher starts it with `dotnet exec` and the target app's deps and runtimeconfig.
  The host then runs on the target's Avalonia version.
  Its window lists the project's AXAML files. It switches between them in the same process.
- `tests/AxView.Tests`: tests for the launcher.

## Work list

Read `TODO.md` before you start. Take the top open item, unless the user gives a different task.
Update `TODO.md` when you finish an item or find new work.

## Language

Write all agent-facing text in ASD-STE100 Simplified Technical English.
This includes comments, commit messages, issues, specs, and reports.

- Use active voice and simple tenses.
- Write one instruction per sentence.
- Keep instructions to 20 words or fewer. Keep descriptions to 25 words or fewer.
- Use one word for one meaning.
- Use a list for three or more steps or conditions.
- Use a full stop, not a semicolon.

## Workflow: TDD

Write the test first. Then write the code.
Make the test fail for the right reason before you write the code.

## Coding standard

Obey all rules in `docs/agents/CODING.md`. Read it before you write or change code.
`.editorconfig` enforces the rules that an analyzer can check. The build fails on a violation.

## Definition of done

A task is done only when all of these conditions are true:

- The code implements the task.
- `dotnet test` passes.
- `dotnet format --verify-no-changes --severity info` shows no errors, warnings, or hints.

## Verify

- Run `dotnet build` and `dotnet test`.
- For a host change, render a real view: `dotnet src/AxView/bin/Debug/net10.0/axview.dll <file.axaml> --snapshot <file.png>`.
- Look at the PNG. A non-zero exit code means the render failed.
