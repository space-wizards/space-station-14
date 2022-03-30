namespace Content.Shared.OpaqueId;

public interface IOpaqueIdManager<T, U>
    where T: unmanaged, IOpaqueId, IEquatable<T>
    where U: IOpaquelyIded<T>
{
    public U GetObjectById(T id);
}
