using Content.Shared._Starlight.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Eye.Blinding.Components;

/// <summary>
///     Component which just enables blinding for entity with this component, from <see cref="ChildBlockVisionComponent"/> parents.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChildBlockVisionSystem))]
public sealed partial class ParentCanBlockVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}