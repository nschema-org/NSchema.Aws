# ![NSchema](https://raw.githubusercontent.com/nschema-org/NSchema.Docs/main/assets/nschema-logo-horizontal.png)

[![NSchema.Aws](https://github.com/nschema-org/NSchema.Aws/actions/workflows/cicd.yml/badge.svg)](https://github.com/nschema-org/NSchema.Aws/actions/workflows/cicd.yml)

# NSchema.Aws

AWS provider for [NSchema](https://github.com/nschema-org/NSchema), the declarative database schema migration tool for .NET. It provides an S3-backed state store, enabling offline migration planning and shared state across a team or CI.

Most users should use the [NSchema CLI](https://github.com/nschema-org/NSchema), which already includes this backend — configure it with a `STATE s3` block. Add this package directly only when [embedding the engine](https://nschema.dev/library/embedding/) in your own application.

## Installation

```sh
dotnet add package NSchema.Core
dotnet add package NSchema.Aws
```

## Documentation

Full documentation lives at **[nschema.dev](https://nschema.dev)**:

- [Amazon S3 backend](https://nschema.dev/backends/s3/) — configuration, library usage, and IAM permissions
- [Offline planning & state](https://nschema.dev/guides/state/)

## License

See [LICENSE](LICENSE).
