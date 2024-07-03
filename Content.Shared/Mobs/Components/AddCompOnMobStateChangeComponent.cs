using Robust.Shared.Prototypes;

namespace Content.Shared.Mobs.Components;

[RegisterComponent]
public sealed partial class AddCompOnMobStateChangeComponent : Component
{
    /// <summary>
    /// On this state the component will be given.
    /// </summary>
    [DataField]
    public MobState MobState = new();

    /// <summary>
    /// Components added to the entity.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}
