# Changelog

All notable changes to NSchema.Aws will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project (mostly) adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning policy

This package uses **lockstep major versioning** with the core NSchema package: `NSchema.Aws X.*.*` requires `NSchema.Core X.*.*`, so version compatibility is always clear.

As a consequence, breaking changes that are specific to this provider (rather than the core API) are signalled by a **minor version bump** rather than a major one, and called out explicitly in this changelog.

## [Unreleased]

### Added

- Added plugin manifest to allow for automatic registration of the backend coming in `NSchema 4.0.0.

## [3.2.0] - 2026-06-25

### Added

- Implemented `IStateLock.Peek` on the S3 state lock: reads the held lock without acquiring it.

### Changed

- Updated to `NSchema 3.4.0`.

## [3.1.0] - 2026-06-24

### Added

- Added ability to configure the `AmazonS3Config` object directly during dependency registration.

## [3.0.0] - 2026-06-20

### Added

- Support for locking using `If-None-Match` to prevent concurrent modifications to the state store.

### Changed

- **Breaking:** Updated to `NSchema 3.0.0`, which includes many breaking changes to the core NSchema API.
- Updated `AWSSDK.S3` from `4.0.23.5` to `4.0.25.2`.

## [2.0.0] - 2026-06-01

Initial version, tracking NSchema 2.0.0.

### Added

- `UseStateStoreS3(...)` builder extensions for configuring an S3 state store with bucket and key.

[3.0.0]: https://github.com/nschema-org/NSchema.Aws/compare/v2.0.0...v3.0.0
[2.0.0]: https://github.com/nschema-org/NSchema.Aws/releases/tag/v2.0.0
