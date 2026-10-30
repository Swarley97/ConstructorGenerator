# ConstructorGenerator 2.0.0

Version 2.0 is a quality release: the generator was rewritten as a proper incremental generator and backed by a snapshot test suite (58 tests). Running that suite against 1.0.2 (`master`) fails 57 of 58 tests. Many of those are cosmetic (output formatting), but the bugs listed below are real.

## Highlights

- Rewritten generation pipeline. It no longer uses Scriban or a text template. Source is emitted directly, so the package ships without the Scriban dependency (1.0.2 pulled in Scriban 5.7.0, which has known vulnerability advisories).
- New diagnostics `CG0001`, `CG0002` and `CG0003`.
- Support for structs, record classes, record structs, generic types, nested types and the global namespace.
- Caching/incremental behavior is covered by tests: changing one member regenerates only the affected output.

## New features

- **Diagnostics**
  - `CG0001` (error): the annotated type is not `partial`. Generation is skipped for it.
  - `CG0002` (warning): the type already declares a constructor with the same signature as the generated one. Generation is skipped for it.
  - `CG0003` (error): the annotated type is `static` and cannot declare an instance constructor. Generation is skipped for it.
  - A constructor with a different signature no longer blocks generation.
- **Structs and record structs.** `[GenerateFullConstructor]` and `[GeneratedConstructorSettings]` now allow `AttributeTargets.Struct` as well as classes.
- **Records.** Record classes and record structs are supported. A record with a primary constructor gets a constructor that chains to `this(...)`.
- **Generic types.** Type parameters and their constraints are emitted for generic types, including multiple type parameters.
- **Array-typed members** (`string[]`, `int[][]`) are included.
- **Nullable annotations are preserved** on parameters (`string?`, `int?`).
- **Accessibility.** The declaring type's accessibility is respected, including nested private types. Abstract classes get a `protected` constructor by default.
- **Base-constructor chaining.**
  - Chaining picks the largest accessible base constructor.
  - Base constructors that are not accessible are not chained.
  - `ref`, `in` and `params` parameters are forwarded correctly.
  - Optional base parameters stay optional after the child's own parameters.
  - Three-level (and deeper) hierarchies chain through every level.
  - A base type whose constructor is declared with a type parameter (`Base<T>`) is resolved to the concrete type.

## Bug fixes

Each of these is covered by a test that fails on 1.0.2 and passes on 2.0.

### Generated code that did not compile
- **Static properties and fields**, and **`const` fields** were included as constructor parameters. They are now ignored (`CS0200`, `CS0131` in 1.0.2).
- **Keywords as member names** (`Class`, `Event`, `Object`) produced parameter names such as `class`. They are now escaped as `@class`, `@event` and `@object`.
- **Nested types** were emitted at namespace level, so the generated code could not see the members (`CS0103`). They are now emitted inside their containing types, and nested private types work.
- **Private and non-public types** were always emitted as `public partial`. The declared accessibility is now used (`CS1527` on nested private types in 1.0.2).
- **Inaccessible base constructors.** Private base constructors were chained anyway (`CS0122`). They are now skipped.
- **`ref`/`in`/`params` base parameters** were forwarded without their modifiers (`CS1620`).
- **Generic types with several type parameters and constraints** produced malformed output (`partial class Sample TKey, TValue`) and 12 compile errors.
- **Non-partial types** (`CS0260`) and **types that already declare the same constructor** (`CS0111`) generated broken code. They now report `CG0001` and `CG0002` instead.

### Generator crashes
- **`IndexOutOfRangeException`** was thrown for short and upper-case member names (test `ShortAndUpperCaseNamesAreHandled`). Such members are now handled.
- **Duplicate hint names.** Types that share a name but differ in arity (`Foo` and `Foo<T>`) threw `ArgumentException`. Every type now gets a unique hint name.
- **Global namespace.** Types in the global namespace threw `ArgumentException` because of an invalid character (`<global namespace>`) in the hint name. They now use `global`.
- Types with the same name in different namespaces get distinct hint names.

### Wrong or missing output
- **Members with a lower-case name** (for example `value`) were assigned as `value = value;`. They are now assigned through `this.`.
- **Parameter name collisions.** A normalized parameter name could clash with another member, for example `_value` and `Value`, or a duplicate name across the inheritance chain. The parameters are now disambiguated (`value`, `value1`).
- **Partial types split across syntax trees** and **types with no dependencies** now generate correctly. An empty type gets a parameterless constructor.
- **Generic record classes** and **records with a primary constructor** generated nothing. They now generate a constructor.
- **Events and non-auto properties** are ignored instead of becoming constructor parameters.
- **Optional parameters** are ordered last and use `default`.

## Internal changes

- The generator is now a stricter incremental generator with value-equatable models (`TypeModel`, `ConstructorInfo`, `ParameterInfo`, `EquatableArray<T>`, `DiagnosticInfo`). Output is regenerated only when relevant input changes.
- `GenerateConstructorTemplate.txt`, `CodeTemplates.cs` and the Scriban dependency are removed.
- The test project moved to NUnit 5 with Verify snapshot testing (`Verify.SourceGenerators`), targeting .NET 10. The old `Tests.cs` and its test classes were replaced by the snapshot suites (accessibility, inheritance, member selection, member types, parameter naming, type shape, diagnostics, incremental).
- Package version is now 2.0.0.

## Upgrade notes

- Generated code changes in whitespace and layout. Type declarations now follow the actual accessibility of the type, and parameter names are escaped or disambiguated differently in the cases above. If you compare generated output, expect diffs.
- Types that are not `partial` now produce the error `CG0001`.
- Declare any type that carries `[GenerateFullConstructor]` as `partial`.
