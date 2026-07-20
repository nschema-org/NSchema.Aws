using System.Text;
using Microsoft.Extensions.Options;
using NSchema.Aws.State;
using NSchema.Aws.Tests.Fixtures;
using NSchema.State.Locks;

namespace NSchema.Aws.Tests.State;

[Collection("minio")]
public sealed class S3SchemaStateStoreTests(MinioFixture fixture)
{
    private S3SchemaStateStore CreateSut(string? key = null) => new(
        Options.Create(new S3SchemaStateStoreOptions
        {
            Bucket = fixture.BucketName,
            Key = key ?? $"state/{Guid.NewGuid():N}.json",
        }),
        fixture.S3);

    private static ReadOnlyMemory<byte> Payload(string content) => Encoding.UTF8.GetBytes(content);

    [Fact]
    public async Task Read_MissingObject_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.Read(TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Write_ThenRead_RoundTripsTheState()
    {
        var sut = CreateSut();
        var original = Payload("""{"schema":"v1"}""");

        await sut.Write(original, TestContext.Current.CancellationToken);
        var result = await sut.Read(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Value.ToArray().ShouldBe(original.ToArray());
    }

    [Fact]
    public async Task Write_OverwritesExistingObject()
    {
        var key = $"state/{Guid.NewGuid():N}.json";
        var sut = CreateSut(key);
        var second = Payload("""{"schema":"v2"}""");

        await sut.Write(Payload("""{"schema":"v1"}"""), TestContext.Current.CancellationToken);
        await sut.Write(second, TestContext.Current.CancellationToken);
        var result = await sut.Read(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Value.ToArray().ShouldBe(second.ToArray());
    }

    [Fact]
    public async Task Acquire_WhenUnlocked_ReturnsHandle()
    {
        var sut = CreateSut();

        var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        handle.Info.Id.Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Acquire_WhenAlreadyLocked_ThrowsWithExistingLockInfo()
    {
        var sut = CreateSut();
        var first = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        var ex = await Should.ThrowAsync<StateLockedException>(
            () => sut.Acquire(new StateLockRequest("destroy"), TestContext.Current.CancellationToken));

        ex.ExistingLock.ShouldNotBeNull();
        ex.ExistingLock.Operation.ShouldBe("apply");
        ex.ExistingLock.Id.ShouldBe(first.Info.Id);
    }

    [Fact]
    public async Task Acquire_AfterReleasingHandle_Succeeds()
    {
        var key = $"state/{Guid.NewGuid():N}.json";
        var sut = CreateSut(key);

        var first = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);
        await first.Release(TestContext.Current.CancellationToken);

        var second = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);
        second.Info.Id.ShouldNotBe(first.Info.Id);
    }

    [Fact]
    public async Task ReleaseHandle_IsIdempotent()
    {
        var sut = CreateSut();
        var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        await handle.Release(TestContext.Current.CancellationToken);
        await Should.NotThrowAsync(async () => await handle.Release(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Release_WhenLocked_RemovesLock()
    {
        var sut = CreateSut();
        await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        await sut.Release(TestContext.Current.CancellationToken);

        // The lock is now free to re-acquire.
        (await sut.Peek(TestContext.Current.CancellationToken)).ShouldBeNull();
        var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);
        handle.Info.Id.Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Release_WhenNotLocked_DoesNothing()
    {
        var sut = CreateSut();

        await Should.NotThrowAsync(async () => await sut.Release(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Peek_WhenLocked_ReturnsInfoWithoutRemovingTheLock()
    {
        var sut = CreateSut();
        var handle = await sut.Acquire(new StateLockRequest("apply"), TestContext.Current.CancellationToken);

        var info = await sut.Peek(TestContext.Current.CancellationToken);

        info.ShouldNotBeNull();
        info.Operation.ShouldBe("apply");
        info.Id.ShouldBe(handle.Info.Id);

        // Peek is read-only: the lock is still held, so a fresh acquire is rejected.
        await Should.ThrowAsync<StateLockedException>(
            () => sut.Acquire(new StateLockRequest("destroy"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Peek_WhenNotLocked_ReturnsNull()
    {
        var sut = CreateSut();

        var info = await sut.Peek(TestContext.Current.CancellationToken);

        info.ShouldBeNull();
    }
}
