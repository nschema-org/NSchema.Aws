using NSchema.Configuration.Plugins;
using NSchema.Plugins;
using NSchema.Project.Nsql;
using NSchema.Project.Nsql.Syntax;
using NSchema.Project.Nsql.Syntax.Blocks;
using NSchema.Project.Nsql.Tokens;

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
    public BlockStatement GetScaffoldTemplate(ScaffoldContext context)
    {
        var key = context.EnvironmentName is { } environment ? $"{environment}/nschema.state.json" : "nschema.state.json";
        var block = new BlockStatement(BlockKeyword.State, Identifier.Synthetic(Source), new SeparatedSyntaxList<BlockAttribute>(
        [
            new BlockAttribute("bucket", "my-nschema-state"),
            new BlockAttribute("key", key),
        ]));

        // The base configuration explains where AWS credentials come from; an environment overlay only restates the
        // block to override the key, so it stays terse.
        return context.EnvironmentName is null
            ? block with
            {
                DocComment = new Token(
                    TokenKind.DocComment,
                    "Credentials come from the standard AWS chain (environment, shared profile, or\ninstance role), not from this block.",
                    SourcePosition.None),
            }
            : block;
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
