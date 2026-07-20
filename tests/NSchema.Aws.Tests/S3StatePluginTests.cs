using NSchema.Plugins;
using NSchema.State.Backends;

namespace NSchema.Aws.Tests;

/// <summary>
/// Pins <see cref="S3StatePlugin"/>'s configuration parsing and validation — the first state-backend manifest.
/// Pure unit tests — no Docker, no AWS calls (registration is lazy).
/// </summary>
public sealed class S3StatePluginTests
{
    private readonly S3StatePlugin _sut = new();

    [Fact]
    public void GetScaffoldTemplate_ReturnsStateBlock()
        => _sut.GetScaffoldTemplate(new ScaffoldContext()).ShouldContain("STATE s3");

    [Fact]
    public void GetScaffoldTemplate_ForEnvironment_NamespacesTheKey()
    {
        // An environment overlay restates the state block with an environment-scoped key so each environment keeps
        // its own state object.
        var overlay = _sut.GetScaffoldTemplate(new ScaffoldContext { EnvironmentName = "prod" });

        overlay.ShouldContain("key     = 'prod/nschema.state.json'");
    }

    [Fact]
    public void Configure_ValidBucketAndKey_SucceedsAndRegistersStateStore()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")));

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        builder.Services.ShouldContain(d => d.ServiceType == typeof(IDatabaseStateStore));
    }

    [Fact]
    public void Configure_AcceptsForcePathStyle()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")),
            ("force_path_style", ConfigValue.OfBoolean(true)));

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Configure_MissingBucketAndKey_AggregatesBothErrors()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config();

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("bucket is required"));
        result.Errors.ShouldContain(e => e.Message.Contains("key is required"));
    }

    [Fact]
    public void Configure_UnknownAttribute_Fails()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")),
            ("nonsense", ConfigValue.OfString("x")));

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("nonsense"));
    }

    [Fact]
    public void Configure_NonBooleanForcePathStyle_Fails()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config(
            ("bucket", ConfigValue.OfString("my-state")),
            ("key", ConfigValue.OfString("nschema.state.json")),
            ("force_path_style", ConfigValue.OfString("yes")));

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("force_path_style"));
    }

    private static PluginConfig Config(params (string Key, ConfigValue Value)[] attributes)
        => new("s3", attributes.ToDictionary(a => new AttributeKey(a.Key), a => a.Value));
}
