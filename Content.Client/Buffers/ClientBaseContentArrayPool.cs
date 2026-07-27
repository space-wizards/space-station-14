using Content.Shared.Buffers;

namespace Content.Client.Buffers;

public sealed class ClientBaseContentArrayPool<T> : BaseContentArrayPool<T>
{
    private readonly ContentArrayPool<T> _arrayPool;

    public ClientBaseContentArrayPool(int startArraySize, int startBucketSize, Func<T>? factory = null, bool init = false)
    {
        _arrayPool = new(startArraySize, startBucketSize, factory, init);
    }

    public override T[] Rent(int minSize)
    {
        return _arrayPool.Rent(minSize);
    }

    public override void Return(T[] obj)
    {
        _arrayPool.Return(obj);
    }
}
