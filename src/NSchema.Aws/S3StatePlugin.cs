using NSchema.Plugins;

namespace NSchema.Aws;

/// <summary>
/// The NSchema plugin manifest for the Amazon S3 state-store backend.
/// </summary>
public sealed class S3StatePlugin : INSchemaStatePlugin
{
    private const string Source = "s3";

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
        var errors = new List<Diagnostic>();
        var bucket = "";
        var key = "";
        var forcePathStyle = false;

        // 'key' is an S3 attribute name here, so the loop variable is named 'attribute' to avoid shadowing it.
        foreach (var (attribute, value) in config.Attributes)
        {
            if (attribute == "bucket")
            {
                bucket = value.AsString();
            }
            else if (attribute == "key")
            {
                key = value.AsString();
            }
            else if (attribute == "force_path_style")
            {
                if (value.Kind is ConfigValueKind.Boolean)
                {
                    forcePathStyle = value.AsBoolean();
                }
                else
                {
                    errors.Add(Diagnostic.Error(Source, "STATE s3: force_path_style must be a boolean."));
                }
            }
            else
            {
                errors.Add(Diagnostic.Error(Source, $"STATE s3: unknown attribute '{attribute}'."));
            }
        }

        if (string.IsNullOrEmpty(bucket))
        {
            errors.Add(Diagnostic.Error(Source, "STATE s3: bucket is required."));
        }

        if (string.IsNullOrEmpty(key))
        {
            errors.Add(Diagnostic.Error(Source, "STATE s3: key is required."));
        }

        if (errors.Count > 0)
        {
            return Result.From(errors);
        }

        builder.UseS3StateStore(bucket, key, clientConfig => clientConfig.ForcePathStyle = forcePathStyle);
        return Result.Success();
    }
}
