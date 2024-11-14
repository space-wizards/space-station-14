namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Blinds entities that are parented to this entity (are in this locker, crate or bag)
/// </summary>
[RegisterComponent]
public sealed partial class ChildBlockVisionComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
