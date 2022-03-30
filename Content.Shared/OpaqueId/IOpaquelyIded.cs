namespace Content.Shared.OpaqueId;

public interface IOpaquelyIded<T>
where T: unmanaged, IOpaqueId, IEquatable<T>
{
    public T? OpaqueId { get; set; }
}
