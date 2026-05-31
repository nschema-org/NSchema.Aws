# Changelog

All notable changes to NSchema.Aws will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project (mostly) adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning policy

This package uses **lockstep major versioning** with the core NSchema package: `NSchema.Aws X.*.*` requires `NSchema X.*.*`, so version compatibility is always clear.

As a consequence, breaking changes that are specific to this provider (rather than the core API) are signalled by a **minor version bump** rather than a major one, and called out explicitly in this changelog.

## [Unreleased] - 2026-05-31

Initial version, tracking NSchema 2.0.0.

### Added

- `S3SchemaStateStore` — an `ISchemaStateStore` implementation that persists the schema snapshot to an S3 object (last-write-wins; locking is out of scope for v1).
- `UseStateStoreS3(bucket, key)` builder extension, with additional overloads accepting `Action<S3SchemaStateStoreOptions>` and `Action<S3SchemaStateStoreOptions, IServiceProvider>` for richer configuration.
- `S3SchemaStateStoreOptions` — configures the target bucket and object key.
- Automatic `AmazonS3Client` registration using ambient credentials (overridable by registering `IAmazonS3` before calling `UseStateStoreS3`).
