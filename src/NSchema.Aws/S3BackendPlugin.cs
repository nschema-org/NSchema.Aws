using NSchema.Configuration;
using NSchema.Plugins;

namespace NSchema.Aws;

/// <summary>
/// The NSchema plugin manifest for the Amazon S3 state-store backend.
/// </summary>
public sealed class S3BackendPlugin : INSchemaBackendPlugin
{
    /// <inheritdoc />
    public string Label => "s3";

    /// <inheritdoc />
    public string GetScaffoldTemplate(ScaffoldContext context)
    {
        var lines = new List<string> { "BACKEND s3 (" };
        if (context.Version is { } version)
        {
            lines.Add($"  version = '{version}',");
        }

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
    public PluginConfigureResult Configure(NSchemaApplicationBuilder builder, ConfigBlock block)
    {
        var errors = new List<string>();
        var bucket = "";
        var key = "";
        var forcePathStyle = false;

        // 'key' is an S3 attribute name here, so the loop variable is named 'attribute' to avoid shadowing it.
        foreach (var (attribute, value) in block.Attributes)
        {
            switch (attribute.ToLowerInvariant())
            {
                case "bucket":
                    bucket = value.AsString();
                    break;
                case "key":
                    key = value.AsString();
                    break;
                case "force_path_style":
                    if (value.Kind is ConfigValueKind.Boolean)
                    {
                        forcePathStyle = value.AsBoolean();
                    }
                    else
                    {
                        errors.Add("BACKEND s3: force_path_style must be a boolean.");
                    }

                    break;
                default:
                    errors.Add($"BACKEND s3: unknown attribute '{attribute}'.");
                    break;
            }
        }

        if (string.IsNullOrEmpty(bucket))
        {
            errors.Add("BACKEND s3: bucket is required.");
        }

        if (string.IsNullOrEmpty(key))
        {
            errors.Add("BACKEND s3: key is required.");
        }

        if (errors.Count > 0)
        {
            return PluginConfigureResult.Failure([.. errors]);
        }

        builder.UseS3StateStore(bucket, key, config => config.ForcePathStyle = forcePathStyle);
        return PluginConfigureResult.Success;
    }
}
