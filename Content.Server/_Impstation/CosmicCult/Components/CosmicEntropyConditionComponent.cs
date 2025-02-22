using Content.Server._Impstation.CosmicCult;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class CosmicEntropyConditionComponent : Component
{
    [DataField] public int Siphoned = 0;
}
