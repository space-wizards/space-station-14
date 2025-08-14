using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._Starlight.NullSpace;

[RegisterComponent, NetworkedComponent]
public sealed partial class NullSpaceComponent : Component
{
    public List<ProtoId<NpcFactionPrototype>> SuppressedFactions = new();
}
