using Content.Server.Radio.EntitySystems;

namespace Content.Server.Radio.Components;

/// <summary>
/// When activated (<see cref="ActiveRadioJammerComponent"/>) prevents from sending messages in range
/// </summary>
[RegisterComponent]
[Access(typeof(JammerSystem))]
public sealed partial class RadioJammerComponent : Component
{
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 8f;

    /// <summary>
    /// Power usage per second when enabled
    /// </summary>
    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 2f;
}
