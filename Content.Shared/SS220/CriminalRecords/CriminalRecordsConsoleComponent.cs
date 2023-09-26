// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Radio;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CriminalRecords;

[RegisterComponent]
public sealed partial class CriminalRecordsConsoleComponent : Component
{
    public (NetEntity, uint)? ActiveKey { get; set; }

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsSecurity = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxMessageLength = 200;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EditCooldown = TimeSpan.FromSeconds(5);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? LastEditTime;

    [DataField]
    public SoundSpecifier DatabaseActionSound = new SoundPathSpecifier(
        "/Audio/SS220/Consoles/SecurityConsoleAction.ogg",
        new()
        {
            Variation = 0.125f
        }
    );

    [DataField]
    public SoundSpecifier KeySwitchSound = new SoundCollectionSpecifier("Keyboard");

    [DataField]
    public ProtoId<RadioChannelPrototype> ReportRadioChannel = "Security";
}

[Serializable, NetSerializable]
public enum CriminalRecordsUiKey
{
    Key,
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CriminalRecordShort
{
    [DataField(required: true)]
    public string Name;

    [DataField]
    public ProtoId<JobPrototype>? JobPrototype;

    [DataField("dna")]
    public string DNA = "";

    [DataField]
    public string Fingerprints = "";

    [DataField]
    public CriminalRecord? LastCriminalRecord;

    [DataField]
    public bool IsInCryo;

    public CriminalRecordShort(string name)
    {
        Name = name;
    }

    public CriminalRecordShort(GeneralStationRecord record, bool includeCriminalRecords = true)
    {
        Name = record.Name;
        JobPrototype = record.JobPrototype;
        DNA = record.DNA ?? "";
        Fingerprints = record.Fingerprint ?? "";
        LastCriminalRecord = null;
        IsInCryo = record.IsInCryo;

        if (!includeCriminalRecords)
            return;

        if (record.CriminalRecords is not null && record.CriminalRecords.LastRecordTime.HasValue)
        {
            if (record.CriminalRecords.Records.TryGetValue(record.CriminalRecords.LastRecordTime.Value, out var criminalRecord))
                LastCriminalRecord = criminalRecord;
        }
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordConsoleState : BoundUserInterfaceState
{
    /// <summary>
    ///     Current selected key.
    /// </summary>
    public (NetEntity, uint)? SelectedKey { get; }
    public GeneralStationRecord? SelectedRecord { get; }
    public Dictionary<(NetEntity, uint), CriminalRecordShort>? RecordListing { get; }
    public CriminalRecordConsoleState(
        (NetEntity, uint)? key,
        GeneralStationRecord? record,
        Dictionary<(NetEntity, uint), CriminalRecordShort>? recordListing)
    {
        SelectedKey = key;
        SelectedRecord = record;
        RecordListing = recordListing;
    }

    public bool IsEmpty() => SelectedKey == null
        && SelectedRecord == null && RecordListing == null;
}

[Serializable, NetSerializable]
public sealed class UpdateCriminalRecordStatus : BoundUserInterfaceMessage
{
    public readonly ProtoId<CriminalStatusPrototype>? StatusTypeId;
    public readonly string Message;

    public UpdateCriminalRecordStatus(string message, ProtoId<CriminalStatusPrototype>? statusTypeId)
    {
        Message = message;
        StatusTypeId = statusTypeId;
    }
}

[Serializable, NetSerializable]
public sealed class DeleteCriminalRecordStatus : BoundUserInterfaceMessage
{
    public readonly int Time;

    public DeleteCriminalRecordStatus(int time)
    {
        Time = time;
    }
}
