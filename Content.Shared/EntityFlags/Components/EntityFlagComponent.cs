using Content.Shared.EntityFlags.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.EntityFlags.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(EntityFlagSystem))]
public sealed class EntityFlagComponent : Component
{
    public HashSet<string> Flags = new();

    //Local flags are NOT synced over the network
    public HashSet<string> LocalFlags = new();
}
