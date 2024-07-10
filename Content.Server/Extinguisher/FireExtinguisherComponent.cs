using Content.Shared.Extinguisher;
using Robust.Shared.GameStates;

namespace Content.Server.Extinguisher;

[RegisterComponent]
[Access(typeof(FireExtinguisherSystem))]
public sealed partial class FireExtinguisherComponent : SharedFireExtinguisherComponent
{
}
