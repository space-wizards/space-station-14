using Content.Shared.Radio.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Prevents all non whitelisted radios from sending messages
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedJammerSystem))]
public sealed partial class ActiveRadioJammerComponent : Component
{
}
