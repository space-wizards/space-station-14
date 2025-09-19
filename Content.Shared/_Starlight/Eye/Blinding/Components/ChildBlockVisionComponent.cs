using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Eye.Blinding.Components;

/// <summary>
///     Blinds entities that are parented to this entity (are in this locker, crate or bag), works only on entities with <see cref="ParentCanBlockVisionComponent"/>. 
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChildBlockVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}