using System.Buffers;
using Content.Shared.Buffers;

namespace Content.Server.Buffers;

public sealed class ServerArrayPool<T> : BaseContentArrayPool<T>
{
    public override T[] Rent(int minSize)
    {
        return ArrayPool<T>.Shared.Rent(minSize);
    }

    public override void Return(T[] obj)
    {
        ArrayPool<T>.Shared.Return(obj);
    }
}
