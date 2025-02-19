using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HeadphonesSystem))]
public sealed partial class HeadphonesComponent : Component
{
    // At the moment this is a very simple item so that is why this component class is empty currently
}
