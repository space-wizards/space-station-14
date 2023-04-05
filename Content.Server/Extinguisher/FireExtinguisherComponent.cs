using Content.Shared.Extinguisher;
using Robust.Shared.GameStates;

namespace Content.Server.Extinguisher;

[NetworkedComponent, RegisterComponent]
[Access(typeof(FireExtinguisherSystem))]
public sealed class FireExtinguisherComponent : SharedFireExtinguisherComponent
{
}
