---
name: dotnet-developer
description: Strict guidelines for .NET 10: SOLID, TDD, Clean Architecture, and DDD.
triggers: [backend, csharp, dotnet, repository, service, controller, test, xunit, refactor, aggregate, domain]
---

# .NET 10 & Architecture Rules

**1. TDD First (xUnit, Moq/NSubstitute)**
- Write tests *before* implementation.
- Use AAA pattern. Naming: `Method_State_ExpectedBehavior`.
- Mock infrastructure; never test DBs/APIs directly.

**2. Strict SOLID**
- **SRP:** Max 3-4 dependencies per service.
- **OCP:** Use polymorphism/Strategy over large `switch`/`if` blocks.
- **LSP:** No `NotImplementedException`. No `is`/`as` type-checking on base classes.
- **ISP:** Keep interfaces lean.
- **DIP:** Depend on abstractions, not concrete implementations.

**3. Clean Architecture & DDD**
- **Layers:** Domain (zero dependencies) <- Application <- Infrastructure.
- **CQRS (MediatR):** Strictly separate Read operations (Queries) from Write operations (Commands) in the Application layer.
- **Aggregates:** Modify data *only* via Aggregate Roots. Reference roots by ID.
- **Encapsulation:** No public setters. Use `init`/`private` setters and behavior methods (`RecordTransaction()`).
- **Value Objects:** Use `record` to encapsulate primitives (e.g., `Money`).
- **Repositories & UoW:** One repo per Aggregate Root. Never leak `IQueryable`. Repos never call `SaveChanges`; rely on `IUnitOfWork`.
- **Domain Events:** Use `IDomainEvent` for side effects.

**4. Idiomatic C# 14**
- Use `var` *only* when the type is obvious.
- Prefer primary constructors, pattern matching, and collection expressions (`[]`).
