using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class StationAIData
{
    [DataField]
    public string Name { get; set; } = "";

    [DataField]
    public ProtoId<StationAIScreenPrototype> Screen { get; set; } = "Default";

    [DataField]
    public ProtoId<SiliconLawsetPrototype> Lawset { get; set; } = "Crewsimov";

    public StationAIData(string name, string screen, string lawset)
    {
        Name = name;
        Screen = screen;
        Lawset = lawset;
    }

    public StationAIData(StationAIData other)
        : this(other.Name, other.Screen, other.Lawset)
    {
    }

    public StationAIData WithName(string name)
    {
        return new(this)
        {
            Name = name
        };
    }

    public StationAIData WithScreen(string screen)
    {
        return new(this)
        {
            Screen = screen
        };
    }

    public StationAIData WithLawset(string lawset)
    {
        return new(this)
        {
            Lawset = lawset
        };
    }

    public bool MemberwiseEquals(StationAIData other)
    {
        return Name == other.Name && Screen == other.Screen && Lawset == other.Lawset;
    }
}

