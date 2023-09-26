// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CriminalRecords;

[Prototype("criminalStatus")]
public sealed class CriminalStatusPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    private string _name = string.Empty;
    public string Name => Loc.GetString(_name);

    [DataField("radioReportMessage")]
    public string? RadioReportMessage;

    /// <summary>
    /// Color of the criminal status when displayed in UIs
    /// </summary>
    [DataField]
    public Color Color = Color.LightGray;

    /// <summary>
    /// The icon that's displayed on the entity and in the UIs
    /// </summary>
    [DataField]
    public ProtoId<StatusIconPrototype>? StatusIcon = null;
}

