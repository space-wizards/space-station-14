using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.EntityEffects.Components;

[RegisterComponent]
public sealed partial class DissolvableComponent : Component
{
    # region Resisting
    [DataField]
    public bool Resisting;
    
    [DataField]
    public TimeSpan? ResistingStartedOn = null;
    
    [DataField]
    public TimeSpan ResistingTime = TimeSpan.FromSeconds(2);
    # endregion
    
    # region Update
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan LastTimeUpdated = TimeSpan.Zero;
    # endregion
    
    [DataField]
    public EntityUid? Effect = null;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool OnDissolve;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float DissolveStacks;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaximumDissolveStacks = 10f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MinimumDissolveStacks = -10f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string DissolvableFixtureID = "dissolvable";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MinDissolveTemperature = 373.15f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool CanResistDissolve { get; private set; } = false;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new(); // Empty by default, we don't want any funny NREs.

    /// <summary>
    ///     Used for the fixture created to handle passing dissolvestacks when two dissolvable objects collide.
    /// </summary>
    [DataField]
    public IPhysShape DissolvableCollisionShape = new PhysShapeCircle(0.35f);

    /// <summary>
    ///     Should the component be set on fire by interactions with isHot entities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool AlwaysCombustible = false;

    /// <summary>
    ///     Can the component anyhow lose its DissolveStacks?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool CanExtinguish = true;

    /// <summary>
    ///     How many DissolveStacks should be applied to component when being set on fire?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float DissolveStacksOnIgnite = 2.0f;

    /// <summary>
    /// Determines how quickly the object will fade out. With positive values, the object will flare up instead of going out.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DissolveStacksFade = -0.1f;
}