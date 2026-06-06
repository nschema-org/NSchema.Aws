# NSchema.Aws

AWS provider for [NSchema](https://github.com/tom-wolfe/NSchema), the declarative database schema migration library for .NET.

This package plugs an S3-backed implementation of NSchema's `ISchemaStateStore` into your application, enabling offline migration planning without live database access.

## Getting started

Install the core package and this provider:

```bash
dotnet add package NSchema
dotnet add package NSchema.Aws
```

Register the S3 state store against an `NSchemaApplicationBuilder`:

```csharp
using NSchema;
using NSchema.Aws;

var builder = NSchemaApplication.CreateBuilder(args);

builder
    .AddSchemasFromAssemblyContaining<Program>()
    .UseCurrentSchemaPostgres(connectionString)
    .UseS3StateStore("my-bucket", "nschema/state.json");

var app = builder.Build();
await app.Apply();
```

## Configuration

`UseStateStoreS3` has three overloads:

```csharp
// 1. Bucket and key directly.
builder.UseStateStoreS3("my-bucket", "nschema/state.json");

// 2. Configure options via a delegate.
builder.UseStateStoreS3(o =>
{
    o.Bucket = configuration["NSchema:Bucket"]!;
    o.Key = configuration["NSchema:Key"]!;
});

// 3. As above, with access to the IServiceProvider.
builder.UseS3StateStore((o, sp) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    o.Bucket = config["NSchema:Bucket"]!;
    o.Key = config["NSchema:Key"]!;
});
```

By default an `AmazonS3Client` using ambient credentials (ECS task role, Lambda execution role, environment variables, etc.) is registered automatically. To bring your own client:

```csharp
// Register via AWSSDK.Extensions.NETCore.Setup before calling UseStateStoreS3.
builder.Services.AddAWSService<IAmazonS3>();
builder.UseS3StateStore("my-bucket", "nschema/state.json");
```

## IAM permissions

| Role             | Required permission            |
|------------------|--------------------------------|
| Deploy / Refresh | `s3:GetObject`, `s3:PutObject` |
| Plan (PR build)  | `s3:GetObject`                 |

Example policy (scoped to the state object):

```json
{
  "Effect": "Allow",
  "Action": ["s3:GetObject", "s3:PutObject"],
  "Resource": "arn:aws:s3:::my-bucket/nschema/state.json"
}
```

## Locking

State writes are last-write-wins. Concurrent applies will silently overwrite each other's state. This is acceptable when deploys are serialised (e.g. a single ECS task per environment).

## License

See [LICENSE](LICENSE).
