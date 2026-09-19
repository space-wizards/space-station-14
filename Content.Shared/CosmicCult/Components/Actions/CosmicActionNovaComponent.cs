using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]
// [AutoGenerateComponentPause]
public sealed partial class CosmicActionNovaComponent : Component
{
    [DataField]
    public EntProtoId Projectile = "ProjectileCosmicNova";

    [DataField]
    public float ProjectileSpeed = 5f;
}
