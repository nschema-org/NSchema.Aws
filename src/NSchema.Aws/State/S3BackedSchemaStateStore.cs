using NSchema.Schema;
using NSchema.State;

namespace NSchema.Aws.State;

internal sealed class S3BackedSchemaStateStore : ISchemaStateStore
{
    public Task<DatabaseSchema?> Read(CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();

    public Task Write(DatabaseSchema schema, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
}
