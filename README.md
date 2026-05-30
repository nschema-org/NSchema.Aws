# NSchema.Aws

AWS provider for [NSchema](https://github.com/tom-wolfe/NSchema), the declarative database schema migration library for .NET.

This package plugs AWS-specific implementations of NSchema's `ISchemaStateStore` (backend schema state store) into your application for storing state in AWS S3.

## Getting started

Install the core package and this provider:

```bash
dotnet add package NSchema
dotnet add package NSchema.Aws
```

Register the state store against an `NSchemaApplicationBuilder`:

```csharp
using NSchema;
using NSchema.Aws;

var builder = NSchemaApplication.CreateBuilder(args);

builder
    .AddSchemasFromAssemblyContaining<Program>()
    .UseAwsS3SchemaStateStore();

var app = builder.Build();
await app.Apply();
```

## Configuration

```csharp
// Bring your own S3 connection.
builder.Services.AddS3();
builder.UseAwsS3SchemaStateStore();
```

## License

See [LICENSE](LICENSE).
