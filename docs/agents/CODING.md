# CODING.md

Mandatory rules for source code in AxView.
The rules come from DAT4000 `docs/agents/CODING.md` and the SPS software wiki "Pravidla pro tvorbu a úpravu zdrojového kódu".
The C# rules follow [csharpcodingguidelines.com](https://csharpcodingguidelines.com).
If a rule does not answer your question, ask.

## Basic principles

The best code is the code you do not write. Justify each line before you write it.

- Solve only the task you received (YAGNI). The customer does not pay for "maybe useful".
- Choose the simplest solution that works (KISS). Write code that a person without deep problem knowledge can read.
- Extract a routine when the same logic occurs a second time (DRY).
- Use object-oriented design. The class is the basic unit of the system. Do not mix in functional style.
- Write the tests before the implementation (TDD).
- Keep all code in Git. Keep each change isolated and visible in the history.
- Use what the platform, framework, or codebase already gives you. Search before you write a new helper.
- Delete dead code. Do not comment it out.
- Keep the diff small. Change only the lines that the task needs.

## Naming

- Use English names. Use full words, not abbreviations (except common ones: `UI`, `Id`, `Xml`, `lbl`).
- Use `PascalCase` for types, methods, properties, and constants. Use `camelCase` for locals, parameters, and private fields.
- Use clear, descriptive names with one interpretation: `SiteSecurityConfig`, not `SiteSecurity`.
- Use plural names for collections: `GetElements`, not `GetElement`.
- Use no type or scope prefixes: `UserName`, not `fUserName`.
- Prefix exceptions: interfaces (`IDevice`), type parameters (`TItem`), events (`OnClick`), UI element names (`btnOk`).
- Name the meaning, not the content: `ExecutionState`, not `EnumExecutionState`. `SystemUser`, not `ClassSystemUser`.
- Do not use names that look like other names: `b00l`, `lO1`.
- Do not repeat the class name in a member: `Employee.Get`, not `Employee.GetEmployee`.
- Use the names that the codebase already uses for the same operation: `Add`, `Count`, `Remove`.
- Name booleans positively with a verb: `IsConnected`, `CanStart`, `SupportsUnlock`. Never `IsNotConnected`.
- Put a specific return type in the method name: `GetCacheLevel()`, `GetConnection()`.
- Add the suffix `Async` to each asynchronous method.
- Use `_` for unused lambda parameters: `(_, _) => HandleClick()`.
- Name each assembly after the namespace that it contains: `AxView.Host`.
- Name each file after the one type that it contains: `CommunicationInterface.cs`.

## Comments

- Add an XML documentation comment (`///`) to each type, method, and property. Write it in English.
- Add a usage example to the documentation comment when the use is not clear.
- Document the structure of each XML or other external data format that the code reads or writes.
- Write a comment to tell why the code does something, when the code does not show it.
- Do not write a comment that repeats the code: `steps.Add(newStep); // add the step to the steps`.

## Readability

- Keep nesting to 5 levels or fewer. Use guard clauses to reduce nesting.
- Make each member `private` by default. Make internal types `internal sealed`. Open access only when a caller needs it.
- Prefer a method to direct field access: `order.SetCustomer("Novak")`, not `order.customer.name = "Novak"`.
- Replace magic numbers and strings with named, documented constants or enums.
- Use explicit types. Never use `var`.
- Declare a local variable where you first use it.
- Initialize a full object or collection in one expression, not property by property.
- Write `if (isConnected)`, not `if (isConnected == true)`.
- Return a boolean expression directly. Do not wrap it in `if`/`else`.
- Use `is` for type tests, not `as` with a `null` check.
- Keep the method parameter count low. Use a parameter object for complex input.
- Put intermediate results in named local variables. This makes debugging easier. Fluent APIs are an exception.

Bad (hard to debug):

```csharp
string result = ConvertToXml(ApplyTransform(ExecuteQuery(GetConfig(dbSource))));
```

## Formatting

- Keep lines to about 130 characters.
- Write one statement per line.
- Indent with 4 spaces. Do not use tabs.
- Put a space after a keyword (`if`, `while`) and around an operator (`=`, `+=`).
- Do not put spaces inside parentheses.
- Always use braces after `if`, `else`, `for`, `foreach`, `while`, and `do`, also for one statement.
- Do not indent initializer blocks. Indent only the continuation lines of a long initializer value.
- Use parentheses to show operator priority in logical expressions.
- Write a LINQ expression on one line if possible. For a long expression, align each operation at the same indent.
- Separate members with one empty line.
- Put one type in one file.

Order members in this sequence:

1. Private constants and fields.
2. Public constants and properties (read-only first).
3. Factory methods.
4. Constructors and finalizers.
5. Events.
6. Other members, in order of logical use: `Init`, `Open`, `Send`, `Receive`, `Close`.

## Class and interface design

- **Single responsibility.** One class has one purpose. One method does one thing.
- **Open/closed.** Add behaviour with new types. Keep stable code unchanged.
- **Liskov substitution.** A subclass must work everywhere its base class works. Do not hide or change base behaviour.
- **Interface segregation.** Use many small interfaces, not one large interface.
- **Dependency inversion.** Depend on interfaces. Inject dependencies through the constructor.
- Use interfaces as the basic design element. A class has one base class but can implement many interfaces.
- Each class has a defined state and predictable behaviour. The same input always gives the same result.
- Access state through properties or methods that validate it.
- Validate input in setters and constructors. Throw an exception for invalid input.
- Classes must not depend on each other in a cycle. Break a cycle with an interface or a design pattern.

## Property and method design

- A property name tells its meaning: `isActivated`, not `activationStatus`.
- Make properties settable in any order. If the order is important, use a method and hide the property.
- Do not add duplicate or contradictory properties: `IsActive` and `IsInactive`.
- Each method and property does one thing only (principle of least surprise).
- Do not expose internal state through static members. Use static members only for stateless logic.
- Prefer `IEnumerable<T>` or `ICollection<T>` to a specific collection type.
- Never return `null` for `string`, collections, or `Task`. Return an empty value.

## Errors

- Report each failure with an exception. Do not return error codes or magic values.
- Give each exception a message that tells the cause.
- Catch only the exceptions that you expect and can handle. Never swallow an exception silently.
- In async code, catch and log exceptions. A background worker must not stop silently.
- Check for `null` where `null` is possible: optional parameters, event sources, external input.

## C# specifics

- Use C# aliases: `int`, `string`, not `Int32`, `String`.
- Use language syntax: `(int, string)`, `DateTime?`, not `Tuple<int, string>`, `Nullable<DateTime>`.
- Put configuration values in configuration files, not in code.
- Use lambdas, not anonymous methods: `items.FirstOrDefault(item => item.Name == name)`.
- Use `async`/`await` for asynchronous work.
- Use `this` and `base` only when the compiler requires them.

## Host

- The host runs inside the target app's runtime. Do not add package references with runtime assets to `AxView.Host`.
- `AxView.Host` compiles against the Avalonia 11.0 API. `AxView.Host12` compiles the same sources against the Avalonia 12.0 API.
- Write host code that compiles against both APIs. Do not use `#if`. ADR 0002 gives the reason.
- Bind an API that only a newer Avalonia version has by reflection, and keep a fallback.
- Do not run the target app's startup code. Import only what `Application.Initialize` loads.

## Portability

- Keep all code OS-agnostic. Build paths with `Path`. Never hard-code a separator, a drive, or a Windows path.
- Find the `dotnet` muxer through `DotnetHost`. Do not start a bare `dotnet`.

## References

- [C# coding guidelines](https://csharpcodingguidelines.com)
- [Guidelines cheat sheet (PDF)](https://github.com/dennisdoomen/CSharpGuidelines/releases/latest)
- [C# guidelines analyzer](https://github.com/bkoelman/CSharpGuidelinesAnalyzer)
- [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
