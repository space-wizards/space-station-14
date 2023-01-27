using Content.Shared.Mech.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Mech;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedMechComponent))]
public sealed partial class MechComponent : SharedMechComponent
{

}
