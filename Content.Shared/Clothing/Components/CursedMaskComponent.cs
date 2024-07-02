using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// This is used for a mask that takes over the host when worn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CursedMaskComponent : Component
{
    [DataField]
    public int CurrentState;

    /// <summary>
    /// Used to determine visuals
    /// </summary>
    [DataField]
    public int CurseStates = 4;

    /// <summary>
    /// Whether or not the mask is currently attached to an NPC.
    /// </summary>
    [DataField]
    public bool HasNpc;

    [DataField]
    public ProtoId<NpcFactionPrototype> CursedMaskFaction = "SimpleHostile";

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();
}

[Serializable, NetSerializable]
public enum CursedMaskVisuals : byte
{
     State
}
