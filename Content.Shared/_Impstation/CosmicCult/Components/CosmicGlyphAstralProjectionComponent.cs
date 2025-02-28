using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class CosmicGlyphAstralProjectionComponent : Component
{
    [DataField]
    public EntProtoId SpawnProjection = "MobCosmicAstralProjection";

    /// <summary>
    /// The duration of the astral projection
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AstralDuration = TimeSpan.FromSeconds(12);

    public DamageSpecifier ProjectionDamage = new()
    {
        DamageDict = new() {
            { "Asphyxiation", 40 }
        }
    };
}
