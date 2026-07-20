using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NSchema.State.Backends;
using NSchema.State.Locks;
using NSchema.State.Locks.Backends;

namespace NSchema.Aws.State;

/// <summary>
/// An <see cref="IDatabaseStateStore"/> that persists the schema snapshot to an S3 object, and an
/// <see cref="IStateLock"/> that coordinates exclusive access to that state via a sibling lock object.
/// </summary>
internal sealed class S3SchemaStateStore(IOptions<S3SchemaStateStoreOptions> options, IAmazonS3 s3) : IDatabaseStateStore, IStateLock
{
    private string Bucket => options.Value.Bucket;

    private string Key => options.Value.Key;

    private string LockKey => options.Value.Key + ".lock";

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>?> Read(CancellationToken cancellationToken = default)
    {
        var state = await ReadObject(Key, cancellationToken);
        return state;
    }

    /// <inheritdoc />
    public Task Write(ReadOnlyMemory<byte> state, CancellationToken cancellationToken = default) =>
        s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = Key,
            InputStream = new MemoryStream(state.ToArray()),
            ContentType = "application/json",
        }, cancellationToken);

    /// <inheritdoc />
    public async Task<IStateLockHandle> Acquire(StateLockInfo info, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = LockKey,
                InputStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(info)),
                ContentType = "application/json",
                // Atomic create-if-absent: S3 returns 412 if the lock object already exists.
                IfNoneMatch = "*",
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            var existing = await ReadLockInfo(cancellationToken);
            throw new StateLockedException(
                existing is null
                    ? "The state is already locked by another operation."
                    : $"The state is locked by '{existing.Who}' (operation '{existing.Operation}') since {existing.CreatedUtc:u}.",
                existing!);
        }

        return new Handle(this, info);
    }

    /// <inheritdoc />
    public Task<StateLockInfo?> Peek(CancellationToken cancellationToken = default) =>
        ReadLockInfo(cancellationToken);

    /// <inheritdoc />
    public ValueTask Release(CancellationToken cancellationToken = default) =>
        new(ReleaseLock(cancellationToken));

    private async Task<ReadOnlyMemory<byte>?> ReadObject(string key, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = Bucket,
                Key = key,
            }, cancellationToken);

            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<StateLockInfo?> ReadLockInfo(CancellationToken cancellationToken)
    {
        var bytes = await ReadObject(LockKey, cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<StateLockInfo>(bytes.Value.Span);
    }

    private Task ReleaseLock(CancellationToken cancellationToken = default) =>
        s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = Bucket,
            Key = LockKey,
        }, cancellationToken);

    private sealed class Handle(S3SchemaStateStore store, StateLockInfo info) : IStateLockHandle
    {
        private int _released;

        public StateLockInfo Info => info;

        public async ValueTask Release(CancellationToken cancellationToken = default)
        {
            // Release is idempotent: only the first call deletes the lock object.
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                await store.ReleaseLock(cancellationToken);
            }
        }
    }
}
