# Changelog

All notable changes to NSchema.Aws will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project (mostly) adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning policy

This package uses **lockstep major versioning** with the core NSchema package: `NSchema.Aws X.*.*` requires `NSchema X.*.*`, so version compatibility is always clear.

As a consequence, breaking changes that are specific to this provider (rather than the core API) are signalled by a **minor version bump** rather than a major one, and called out explicitly in this changelog.

## [Unreleased]

### Changed

- **Breaking:** Updated to NSchema 3.0.0, which includes many breaking changes to the core NSchema API.
- Updated `AWSSDK.S3` from `4.0.23.5` to `4.0.24.1`.

## [2.0.0] - 2026-06-01

Initial version, tracking NSchema 2.0.0.

### Added

- `UseStateStoreS3(...)` builder extensions for configuring an S3 state store with bucket and key.
