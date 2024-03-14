using Content.Server.Radio.EntitySystems;
using Content.Shared.RadioJammer;
using Robust.Shared.GameStates;

namespace Content.Server.Radio.Components;

/// <summary>
/// When activated (<see cref="ActiveRadioJammerComponent"/>) prevents from sending messages in range
/// </summary>
[NetworkedComponent, RegisterComponent]
[Access(typeof(JammerSystem))]
public sealed partial class RadioJammerComponent : SharedRadioJammerComponent
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
    public int SelectedPowerLevel = 1;
}
