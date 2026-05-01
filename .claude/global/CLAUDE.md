---
tags: [claude, profile]
---

# User Profile

IT architect specializing in .NET backend systems and process optimization.

## Communication style
- Be direct. No filler, no flattery, no obvious advice, no closing summaries on simple tasks.
- Default to concise answers. Expand only when explicitly asked.
- Match the language of the question. Code comments are always English.
- If a request is ambiguous in a way that affects implementation, ask one focused question. Otherwise proceed.

## Code generation rules
- All comments in code MUST be in English regardless of conversation language.
- Prefer working MVP over perfect detailed solution. Iterate.
- Show only the changed parts unless asked for the full file.
- For C#, use modern style: primary constructors, file-scoped namespaces, target-typed `new`, collection expressions, `required` members, raw string literals where helpful.
- Never invent APIs. If unsure of a signature, read the source or say so.

## Default stack assumptions (override per project)
- .NET 10 / C# 14
- ASP.NET Core for web
- EF Core (writes) + Dapper (reads)
- xUnit + Autofixture + NSubstitute + FluentAssertions for tests
- Serilog for logging
- CSharpFunctionalExtensions for `Result<T>`

## Architecture defaults
- Clean Architecture (Domain → ApplicationServices → Infrastructure → Api)
- `Result<T>` for expected failures, exceptions for unexpected
- Domain layer has zero infrastructure dependencies
- All I/O behind interfaces (incl. `IFileSystem`, `IClock`)

## Workflow preferences
- Use Plan mode for any change touching > 3 files or affecting public contracts.
- Before generating non-trivial code, verify the project's `CLAUDE.md` and standards docs are loaded.
- Suggest tests alongside non-trivial code, but write them only when asked.
- For investigation tasks, prefer `Grep`/`Glob` over reading whole files.

## Things to skip unless asked
- "Here is what I will do" preambles
- Restating the question
- Documentation, READMEs, examples
- "Hope this helps" / "Let me know if..." postambles
