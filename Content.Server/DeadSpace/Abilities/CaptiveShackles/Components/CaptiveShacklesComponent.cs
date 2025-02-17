using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;


namespace Content.Server.DeadSpace.Abilities.CaptiveShackles.Components;

[RegisterComponent]
public sealed partial class CaptiveShacklesComponent : Component
{

    [ViewVariables]
    public ProtoId<NpcFactionPrototype>? OldFaction = new();

    [ViewVariables]
    public string OldTask;
}
