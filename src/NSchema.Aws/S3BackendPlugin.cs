using NSchema.Configuration;
using NSchema.Plugins;

namespace NSchema.Aws;

/// <summary>
/// The NSchema plugin manifest for the Amazon S3 state-store backend.
/// </summary>
public sealed class S3BackendPlugin : INSchemaBackendPlugin
{
    private const string Template =
        """
        BACKEND s3 (
          bucket = 'my-nschema-state',
          key    = 'nschema.state.json'
        );
        """;

    /// <inheritdoc />
    public string Label => "s3";

    /// <inheritdoc />
    public string GetScaffoldTemplate(ScaffoldContext context) => Template;

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
