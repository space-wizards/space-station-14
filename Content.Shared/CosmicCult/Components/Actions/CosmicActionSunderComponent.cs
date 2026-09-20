using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]

public sealed partial class CosmicActionSunderComponent : Component
{
    [DataField]
    public TimeSpan SunderPauseTime = TimeSpan.FromSeconds(1.5);

    [DataField]
    public EntProtoId Vfx = "EffectCosmicActionSunder";

    [DataField]
    public EntProtoId AreaEffect = "CosmicSunderAreaEffect";
}
