// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SpiderTerrorRuleSystem))]
public sealed partial class SpiderTerrorRuleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UpdateDuration = TimeSpan.FromMinutes(1);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilErt;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationErt = TimeSpan.FromMinutes(10);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UpdateUtil;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilStartRule = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationStartRule = TimeSpan.FromSeconds(300);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilSendMessage = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationSendMessage = TimeSpan.FromSeconds(100);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilErtAnnouncement = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationErtAnnouncement = TimeSpan.FromSeconds(10);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilDeadSquadAnnouncement = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationDeadSquadAnnouncement = TimeSpan.FromSeconds(10);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilDeadSquadArrival = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationDeadSquadArrival = TimeSpan.FromSeconds(120);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilCodeEpsilon = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationCodeEpsilon = TimeSpan.FromSeconds(30);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsDeadSquadSend = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsDeadSquadArrival = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsCodeEpsilon = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsErtSend = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsErtSendMessage = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int CburnCount = 0;

    [DataField]
    public string Sound = "/Audio/_DeadSpace/Spiders/brif.ogg";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, SpiderTerrorStages> StationStages { get; set; } = new Dictionary<EntityUid, SpiderTerrorStages>();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SendMessageConsole = true;

    // Установить стадию размножения пауков ужаса
    public void StartBreeding(EntityUid stationUid)
    {
        if (StationStages.ContainsKey(stationUid))
        {
            StationStages[stationUid] |= SpiderTerrorStages.Breeding;
        }
        else
        {
            StationStages[stationUid] = SpiderTerrorStages.Breeding;
        }
    }


    // Установить стадию высылается кодов от ядерной боеголовки
    public void SendNuclearCode(EntityUid stationUid)
    {
        if (StationStages.ContainsKey(stationUid))
        {
            StationStages[stationUid] |= SpiderTerrorStages.NuclearCode;
        }
        else
        {
            StationStages[stationUid] = SpiderTerrorStages.NuclearCode;
        }
    }


    // Установить стадию захвата станции пауками ужаса
    public void CaptureStation(EntityUid stationUid)
    {
        if (StationStages.ContainsKey(stationUid))
        {
            StationStages[stationUid] |= SpiderTerrorStages.StationCapture;
        }
        else
        {
            StationStages[stationUid] = SpiderTerrorStages.StationCapture;
        }
    }

    // Проверить, активна ли стадия размножения пауков ужаса
    public bool IsBreedingActive(EntityUid stationUid)
    {
        return StationStages.TryGetValue(stationUid, out var stage) && stage.HasFlag(SpiderTerrorStages.Breeding);
    }

    // Проверить, активна ли стадия высылания кодов от ядерной боеголовки
    public bool IsNuclearCodeActive(EntityUid stationUid)
    {
        return StationStages.TryGetValue(stationUid, out var stage) && stage.HasFlag(SpiderTerrorStages.NuclearCode);
    }

    // Проверить, активна ли стадия захвата станции пауками ужаса
    public bool IsStationCaptureActive(EntityUid stationUid)
    {
        return StationStages.TryGetValue(stationUid, out var stage) && stage.HasFlag(SpiderTerrorStages.StationCapture);
    }

    // Очистить стадию размножения пауков ужаса
    public void ClearBreedingStage(EntityUid stationUid)
    {
        if (StationStages.ContainsKey(stationUid))
        {
            StationStages[stationUid] &= ~SpiderTerrorStages.Breeding;

            // Удаляем запись, если нет активных стадий
            if (StationStages[stationUid] == SpiderTerrorStages.None)
            {
                StationStages.Remove(stationUid);
            }
        }
    }

    // Очистить стадию высылается кодов от ядерной боеголовки
    public void ClearNuclearCodeStage(EntityUid stationUid)
    {
        if (StationStages.ContainsKey(stationUid))
        {
            StationStages[stationUid] &= ~SpiderTerrorStages.NuclearCode;

            // Удаляем запись, если нет активных стадий
            if (StationStages[stationUid] == SpiderTerrorStages.None)
            {
                StationStages.Remove(stationUid);
            }
        }
    }


    // Очистить стадию захвата станции пауками ужаса
    public void ClearStationCaptureStage(EntityUid stationUid)
    {
        if (StationStages.ContainsKey(stationUid))
        {
            StationStages[stationUid] &= ~SpiderTerrorStages.StationCapture;

            // Удаляем запись, если нет активных стадий
            if (StationStages[stationUid] == SpiderTerrorStages.None)
            {
                StationStages.Remove(stationUid);
            }
        }
    }

}

[Flags]
public enum SpiderTerrorStages
{
    None = 0,
    Breeding = 1 << 0,       // 0001 - стадия размножения пауков ужаса
    NuclearCode = 1 << 1,    // 0010 - стадия высылается кодов от ядерной боеголовки
    StationCapture = 1 << 2  // 0100 - стадия захвата станции пауками ужаса
}

[ByRefEvent]
public readonly record struct SpiderTerrorAttackStationEvent(EntityUid Station);
