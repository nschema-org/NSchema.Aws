# CLAUDE.md

## Commands

```bash
# Build
dotnet build

# Run all tests (requires Docker)
dotnet test

# Run a single test class
dotnet test tests/NSchema.Aws.Tests --filter "FullyQualifiedName~S3SchemaStateStoreTests"
```

## Local NSchema dependency

`NSchema.Aws` depends on `NSchema` as a NuGet package resolved from the local cache (`~/.nuget/packages`).
When iterating on both repos together, rebuild and push the updated NSchema package after any core changes:

```bash
cd ../NSchema
dotnet build src/NSchema/NSchema.csproj --no-incremental
rm -rf ~/.nuget/packages/nschema/<version>
dotnet nuget push src/NSchema/bin/Debug/NSchema.<version>.nupkg -s ~/.nuget/packages
cd ../NSchema.Aws
dotnet restore --force
```

## Architecture

`NSchema.Aws` provides an S3-backed schema state store for NSchema. The user registers it via:

```csharp
builder.UseStateStoreS3("my-bucket", "nschema/state.json");
```

The three-overload chain (`bucket+key` → `Action<Options>` → `Action<Options, IServiceProvider>`) follows the same pattern as `UseCurrentSchemaPostgres` in `NSchema.Postgres`.

Serialization is handled by `ISchemaStateSerializer` (injected from DI), which defaults to NSchema's internal `DefaultSchemaStateSerializer`. The store itself is format-agnostic.

## Integration tests

Tests use [Testcontainers.Minio](https://www.nuget.org/packages/Testcontainers.Minio) to spin up a real S3-compatible endpoint. Docker must be running. The `MinioFixture` starts the container, creates a bucket, and resolves `ISchemaStateSerializer` from a built `NSchemaApplication`.
