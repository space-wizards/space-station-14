using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// If a gun with this component fires a projectile with <see cref="TargetFinderHitscanComponent"/>,
/// when the projectile hits a entity. it will update the target in this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TargetFinderComponent : Component
{
    /// <summary>
    /// Target selected
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// Reference to gun with <see cref="TargetAssignComponent"/> connected with this entity
    /// </summary>
    public EntityUid? TargetAssigner;

    /// <summary>
    /// The linking port for linking the targetFinder with the targetAssign.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "TargetSource";
}
