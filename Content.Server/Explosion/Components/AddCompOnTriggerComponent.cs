using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;

/// <summary>
/// A component that add components when a trigger is triggered.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(TriggerSystem))]
public sealed partial class AddCompOnTriggerComponent : Component
{
    [DataField]
    public bool ToSelf = false;

    [DataField]
    public bool ToOther = false;

    [DataField]
    public ComponentRegistry Components = new();
}
