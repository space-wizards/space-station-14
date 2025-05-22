using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Paper;

[RegisterComponent, NetworkedComponent]
public sealed partial class AntagOnSignComponent : Component
{
    [DataField] public float Chance = 1.0f;
    [DataField("spawnParadoxClone")] public bool ParadoxClone = false;
    [DataField] public List<ProtoId<EntityPrototype>> Antags = [];
}