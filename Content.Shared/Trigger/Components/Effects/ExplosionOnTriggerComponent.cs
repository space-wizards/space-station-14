using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

// TODO some sort of struct like DamageSpecifier but for explosions.
/// <summary>
/// Will explode the entity using this component's explosion specifications.
/// If TargetUser is true, they'll explode instead.
/// The User will be logged in the admin logs.
/// </summary>
/// <seealso cref="ExplodeOnTriggerComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <inheritdoc cref="ExplosiveComponent.ExplosionType"/>
    [DataField, AutoNetworkedField]
    public ProtoId<ExplosionPrototype> ExplosionType = SharedExplosionSystem.DefaultExplosionPrototypeId;

    /// <inheritdoc cref="ExplosiveComponent.MaxIntensity"/>
    [DataField, AutoNetworkedField]
    public float MaxTileIntensity = 4;

    /// <inheritdoc cref="ExplosiveComponent.IntensitySlope"/>
    [DataField, AutoNetworkedField]
    public float IntensitySlope = 1;

    /// <inheritdoc cref="ExplosiveComponent.TotalIntensity"/>
    [DataField, AutoNetworkedField]
    public float TotalIntensity = 10;

    /// <inheritdoc cref="ExplosiveComponent.TileBreakScale"/>
    [DataField, AutoNetworkedField]
    public float TileBreakScale = 1f;

    /// <inheritdoc cref="ExplosiveComponent.MaxTileBreak"/>
    [DataField, AutoNetworkedField]
    public int MaxTileBreak = int.MaxValue;

    /// <inheritdoc cref="ExplosiveComponent.CanCreateVacuum"/>
    [DataField, AutoNetworkedField]
    public bool CanCreateVacuum = true;
}
