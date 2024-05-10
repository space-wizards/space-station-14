using Content.Shared.Extinguisher;
using Robust.Shared.GameStates;

namespace Content.Client.Extinguisher;

[NetworkedComponent, RegisterComponent]
public sealed partial class FireExtinguisherComponent : SharedFireExtinguisherComponent
{
}
