namespace Content.Shared.Machines.Events;

/// <summary>
/// This event is raised when the assembled state of a Multipart machine changes.
/// This includes when optional parts are found, parts become unanchored, or move
/// within a construction graph.
/// </summary>
/// <param name="Entity">Entity that is bound to the multipart machine.</param>
/// <param name="IsAssembled">Assembled state of the machine.</param>
/// <param name="User">Optional user that may have caused the assembly state to change.</param>
/// <param name="PartsAdded">Dictionary of keys to entities of parts that have been added to this machine.</param>
/// <param name="PartsRemoved">Dictionary of keys to entities of parts that have been removed from this machine.</param>
[ByRefEvent]
public record struct MultipartMachineAssemblyStateChanged(
    EntityUid Entity,
    bool IsAssembled,
    EntityUid? User,
    Dictionary<Enum, EntityUid> PartsAdded,
    Dictionary<Enum, EntityUid> PartsRemoved)
{
}
