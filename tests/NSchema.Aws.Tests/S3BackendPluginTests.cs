using NSchema.Configuration;
using NSchema.Plugins;
using NSchema.State;

namespace NSchema.Aws.Tests;

/// <summary>
/// Pins <see cref="S3BackendPlugin"/>'s block parsing and validation — the first backend manifest. Pure unit
/// tests — no Docker, no AWS calls (registration is lazy).
/// </summary>
public sealed class S3BackendPluginTests
{
    private readonly S3BackendPlugin _sut = new();

    [Fact]
    public void Label_IsS3() => _sut.Label.ShouldBe("s3");

    [Fact]
    public void GetScaffoldTemplate_ReturnsBackendBlock()
        => _sut.GetScaffoldTemplate(new ScaffoldContext()).ShouldContain("BACKEND s3");

    [Fact]
    public void GetScaffoldTemplate_WithVersion_PinsIt()
        => _sut.GetScaffoldTemplate(new ScaffoldContext { Version = "9.9.9" }).ShouldContain("version = '9.9.9',");

    [Fact]
    public void GetScaffoldTemplate_ForEnvironment_NamespacesTheKey()
    {
        // An environment overlay restates the backend with an environment-scoped key so each environment keeps its
        // own state object.
        var overlay = _sut.GetScaffoldTemplate(new ScaffoldContext { EnvironmentName = "prod" });

        overlay.ShouldContain("key     = 'prod/nschema.state.json'");
    }

    [Fact]
    public void Configure_ValidBucketAndKey_SucceedsAndRegistersStateStore()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var block = Block(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")));

        // Act
        var result = _sut.Configure(builder, block);

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        builder.Services.ShouldContain(d => d.ServiceType == typeof(ISchemaStateStore));
    }

    [Fact]
    public void Configure_AcceptsForcePathStyle()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var block = Block(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")),
            ("force_path_style", ConfigValue.OfBoolean(true)));

        // Act
        var result = _sut.Configure(builder, block);

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Configure_MissingBucketAndKey_AggregatesBothErrors()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var block = Block();

        // Act
        var result = _sut.Configure(builder, block);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("bucket is required"));
        result.Errors.ShouldContain(e => e.Contains("key is required"));
    }

    [Fact]
    public void Configure_UnknownAttribute_Fails()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var block = Block(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")),
            ("nonsense", ConfigValue.OfString("x")));

        // Act
        var result = _sut.Configure(builder, block);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("unknown attribute 'nonsense'"));
    }

    [Fact]
    public void Configure_NonBooleanForcePathStyle_Fails()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var block = Block(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")),
            ("force_path_style", ConfigValue.OfString("yes")));

        // Act
        var result = _sut.Configure(builder, block);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("force_path_style must be a boolean"));
    }

    private static ConfigBlock Block(params (string Key, ConfigValue Value)[] attributes)
        => new("backend", "s3", attributes.ToDictionary(a => a.Key, a => a.Value));
}
