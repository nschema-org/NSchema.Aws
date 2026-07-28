using NSchema.Configuration.Plugins;
using NSchema.Plugins;
using NSchema.Project.Nsql;
using NSchema.Project.Nsql.Syntax.Settings;
using NSchema.State.Plugins;

namespace NSchema.Aws.Tests;

/// <summary>
/// Pins <see cref="S3StatePlugin"/>'s configuration parsing and validation — the first state-backend manifest.
/// Pure unit tests — no Docker, no AWS calls (registration is lazy).
/// </summary>
public sealed class S3StatePluginTests
{
    private readonly S3StatePlugin _sut = new();

    private static SettingsStatement Configured(NsqlDocument document) =>
        document.Statements.OfType<SettingsStatement>().ShouldHaveSingleItem();

    [Fact]
    public void GetScaffoldTemplate_ReturnsStateBlock()
    {
        var block = Configured(_sut.GetScaffoldTemplate(new ScaffoldContext()));

        block.Keyword.ShouldBe(SettingsKeyword.State);
        block.Label!.Value.ShouldBe("s3");
        block.Settings.Single(a => a.Key == "bucket").Value.ShouldBe("my-nschema-state");
    }

    [Fact]
    public void GetScaffoldTemplate_ForEnvironment_CarriesOnlyTheKeyThatMoves()
    {
        // Act — an overlay refines the statement it restates, so each environment keeps its own state object without
        // restating the bucket.
        var overlay = Configured(_sut.GetScaffoldTemplate(new ScaffoldContext { EnvironmentName = "prod" }));

        // Assert
        overlay.Settings.ShouldHaveSingleItem().Key.ShouldBe("key");
        overlay.Settings[0].Value.ShouldBe("prod/nschema.state.json");
    }

    [Fact]
    public void Configure_ValidBucketAndKey_SucceedsAndRegistersStateStore()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config(
            ("bucket", "my-state"),
            ("key", "nschema.state.json"));

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
            ("bucket", "my-state"),
            ("key", "nschema.state.json"),
            ("force_path_style", "true"));

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
            ("bucket", "my-state"),
            ("key", "nschema.state.json"),
            ("nonsense", "x"));

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("nonsense", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Configure_NonBooleanForcePathStyle_Fails()
    {
        // Arrange
        var builder = NSchemaApplication.CreateBuilder();
        var config = Config(
            ("bucket", "my-state"),
            ("key", "nschema.state.json"),
            ("force_path_style", "yes"));

        // Act
        var result = _sut.Configure(builder, config);

        // Assert
        // The binder rejects a value it cannot convert to bool.
        result.IsFailure.ShouldBeTrue();
    }

    private static PluginSettings Config(params (string Key, string? Value)[] attributes)
        => new("s3", attributes.ToDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase));
}
