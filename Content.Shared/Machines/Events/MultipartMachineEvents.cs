namespace Content.Shared.Machines.Events;

/// <summary>
/// This event is raised when the assembled state of a Multipart machine changes.
/// This includes when optional parts are found, parts become unanchored, or move
/// within a construction graph.
/// </summary>
/// <param name="uid">Entity that is bound to the multipart machine</param>
/// <param name="assembled">Assembled state of the machine</param>
public sealed class MultipartMachineAssemblyStateChanged(EntityUid uid, bool assembled) : EntityEventArgs
{
    public readonly EntityUid Entity = uid;
    public readonly bool Assembled = assembled;
}
