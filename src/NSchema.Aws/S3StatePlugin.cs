using NSchema.Plugins;
using NSchema.Plugins.Model.Config;

namespace NSchema.Aws;

/// <summary>
/// The NSchema plugin manifest for the Amazon S3 state-store backend.
/// </summary>
public sealed class S3StatePlugin : INSchemaStatePlugin
{
    private const string Source = "s3";

    /// <summary>The settings a STATE statement binds onto.</summary>
    private sealed class S3Settings
    {
        public string? Bucket { get; set; }
        public string? Key { get; set; }
        public bool ForcePathStyle { get; set; }
    }

    /// <inheritdoc />
    public string GetScaffoldTemplate(ScaffoldContext context)
    {
        var lines = new List<string> { "STATE s3 (" };

        // The base configuration explains where AWS credentials come from; an environment overlay only restates the
        // block to override the key, so it stays terse.
        if (context.EnvironmentName is null)
        {
            lines.Add("  -- Credentials come from the standard AWS chain (environment, shared profile, or");
            lines.Add("  -- instance role), not from this block.");
        }

        lines.Add("  bucket  = 'my-nschema-state',");
        var key = context.EnvironmentName is { } environment ? $"{environment}/nschema.state.json" : "nschema.state.json";
        lines.Add($"  key     = '{key}'");
        lines.Add(");");
        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    public Result Configure(NSchemaApplicationBuilder builder, PluginConfig config)
    {
        var bound = config.Bind<S3Settings>();
        var diagnostics = new List<Diagnostic>(bound.Diagnostics);
        var settings = bound.Value!;

        if (string.IsNullOrEmpty(settings.Bucket))
        {
            diagnostics.Add(Diagnostic.Error(Source, "STATE s3: bucket is required."));
        }

        if (string.IsNullOrEmpty(settings.Key))
        {
            diagnostics.Add(Diagnostic.Error(Source, "STATE s3: key is required."));
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return Result.From(diagnostics);
        }

        builder.UseS3StateStore(settings.Bucket!, settings.Key!, clientConfig => clientConfig.ForcePathStyle = settings.ForcePathStyle);
        return Result.From(diagnostics);
    }
}
