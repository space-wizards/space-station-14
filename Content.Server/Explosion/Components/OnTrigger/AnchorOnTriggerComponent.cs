using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Will anchor the attached entity upon a <see cref="TriggerEvent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class AnchorOnTriggerComponent : Component
{
    [DataField]
    public bool RemoveOnTrigger = true;
}
