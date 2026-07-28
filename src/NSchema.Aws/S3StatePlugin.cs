using NSchema.Configuration.Plugins;
using NSchema.Plugins;
using NSchema.Project.Nsql;
using NSchema.Project.Nsql.Syntax.Settings;

namespace NSchema.Aws;

/// <summary>
/// The NSchema plugin manifest for the Amazon S3 state-store backend.
/// </summary>
public sealed class S3StatePlugin : INSchemaStatePlugin
{
    private const string Source = "s3";
    private const string DefaultBucket = "my-nschema-state";

    /// <inheritdoc />
    /// <remarks>
    /// Only the bucket is asked for: the key follows from the environment, which is the plugin's own knowledge, and
    /// credentials come from the AWS chain rather than from an answer.
    /// </remarks>
    public IReadOnlyList<ScaffoldPrompt> GetScaffoldPrompts(ScaffoldContext context) =>
    [
        new() { Key = "bucket", Label = "S3 bucket", Default = DefaultBucket },
    ];

    /// <inheritdoc />
    public NsqlDocument GetScaffoldTemplate(ScaffoldContext context)
    {
        // An overlay refines the statement it restates, so it carries only the key that moves; the bucket it omits
        // carries through from the base.
        if (context.EnvironmentName is { } environment)
        {
            return new NsqlDocument([SettingsStatement.State(Source).WithSetting("key", $"{environment}/nschema.state.json")]);
        }

        return new NsqlDocument([
            SettingsStatement.State(Source)
                .WithSetting("bucket", context.Answer("bucket") ?? DefaultBucket)
                .WithSetting("key", "nschema.state.json")
                .WithDocComment("Credentials come from the standard AWS chain (environment, shared profile, or\ninstance role), not from this block."),
        ]);
    }

    /// <inheritdoc />
    public Result Configure(NSchemaApplicationBuilder builder, PluginSettings settings)
    {
        var bound = settings.Get<S3Settings>();
        if (bound.Value is not { } options)
        {
            return Result.From(bound.Diagnostics);
        }

        var diagnostics = new List<Diagnostic>(bound.Diagnostics);

        if (string.IsNullOrEmpty(options.Bucket))
        {
            diagnostics.Add(Diagnostic.Error(Source, "STATE s3: bucket is required."));
        }

        if (string.IsNullOrEmpty(options.Key))
        {
            diagnostics.Add(Diagnostic.Error(Source, "STATE s3: key is required."));
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return Result.From(diagnostics);
        }

        builder.UseS3StateStore(options.Bucket!, options.Key!, clientConfig => clientConfig.ForcePathStyle = options.ForcePathStyle);
        return Result.From(diagnostics);
    }
}
