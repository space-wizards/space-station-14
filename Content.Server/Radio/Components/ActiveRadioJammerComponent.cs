using Content.Server.Radio.EntitySystems;

namespace Content.Server.Radio.Components;

/// <summary>
/// Prevents all radio in range from sending messages
/// </summary>
[RegisterComponent]
[Access(typeof(JammerSystem))]
public sealed partial class ActiveRadioJammerComponent : Component
{
}
