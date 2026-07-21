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

## Local NSchema.Core dependency

`NSchema.Aws` depends on `NSchema.Core` as a NuGet package resolved from the local cache (`~/.nuget/packages`).
When iterating on both repos together, rebuild and push the updated Core package after any core changes:

```bash
cd ../NSchema.Core
dotnet build src/NSchema.Core/NSchema.Core.csproj --no-incremental
rm -rf ~/.nuget/packages/nschema.core/<version>
dotnet nuget push src/NSchema.Core/bin/Debug/NSchema.Core.<version>.nupkg -s ~/.nuget/packages
cd ../NSchema.Aws
dotnet restore --force
```

## Architecture

`NSchema.Aws` provides an S3-backed schema state store for NSchema. The user registers it via:

```csharp
builder.UseS3StateStore("my-bucket", "nschema/state.json");
```

The three-overload chain (`bucket+key` → `Action<S3SchemaStateStoreOptions>` → `Action<S3SchemaStateStoreOptions, IServiceProvider>`, each taking an optional `Action<AmazonS3Config>`) follows the same pattern as `UsePostgres` in `NSchema.Postgres`.

`S3SchemaStateStore` is byte-oriented: as the core's `IDatabaseStateStore` it reads and writes the raw state payload as `ReadOnlyMemory<byte>`, and it doubles as the `IStateLock`, coordinating exclusive access through a sibling `.lock` object. Serializing the state snapshot is the core's concern, not the store's.

## Integration tests

Tests use [Testcontainers.Minio](https://www.nuget.org/packages/Testcontainers.Minio) to spin up a real S3-compatible endpoint. Docker must be running. The `MinioFixture` starts the container, creates a bucket, and exposes an `IAmazonS3` client; the tests construct `S3SchemaStateStore` directly against them.
