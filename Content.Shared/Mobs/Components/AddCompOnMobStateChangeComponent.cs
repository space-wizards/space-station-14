using Robust.Shared.Prototypes;

namespace Content.Shared.Mobs.Components;

/// <summary>
/// Used to give the entity certain components when it changes to certain mobstate
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AddCompOnMobStateChangeSystem))]
public sealed partial class AddCompOnMobStateChangeComponent : Component
{
    /// <summary>
    /// On this state the component will be given.
    /// </summary>
    [DataField(required: true)]
    public MobState MobState { get; set; }

    /// <summary>
    /// Components added to the entity.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
