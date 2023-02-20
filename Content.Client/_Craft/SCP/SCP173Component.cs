using Content.Shared.SCP.ConcreteSlab;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Client.SCP.ConcreteSlab
{
    [RegisterComponent]
    [Access(typeof(SCP173System))]
    [ComponentReference(typeof(SharedSCP173Component))]
    public sealed class SCP173Component : SharedSCP173Component { }
}
