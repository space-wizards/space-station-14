using Content.Server.Radio.EntitySystems;

namespace Content.Server.Radio.Components;

/// <summary>
/// When activated (<see cref="ActiveRadioJammerComponent"/>) prevents from sending messages in range
/// </summary>
[RegisterComponent]
[Access(typeof(JammerSystem))]
public sealed partial class RadioJammerComponent : Component
{
    [DataDefinition]
    public partial struct RadioJamSetting
    {
        [DataField(required: true)]
        public float Wattage;

        [DataField(required: true)]
        public float Range;

        [DataField(required: true)]
        public LocId Message = string.Empty;

        [DataField(required: true)]
        public LocId Name = string.Empty;
    }

    [DataField(required: true)]
    public List<RadioJamSetting> Settings = new();

    [DataField]
    public byte SelectedPowerLevel = 1;
}
