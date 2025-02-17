// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Server.Objectives.Systems;
using Robust.Shared.Map;
using Content.Server.Station.Systems;

namespace Content.Server.DeadSpace.Spiders.SpiderTerror;

public sealed class SpiderTerrorConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly StationSystem _station = default!;
    private Dictionary<EntityUid, List<TileRef>> TileRefs { get; set; } = new Dictionary<EntityUid, List<TileRef>>();

    public override void Initialize()
    {
        SubscribeLocalEvent<SpiderTerrorConditionsComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, SpiderTerrorConditionsComponent component, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<TransformComponent>(args.Mind.OwnedEntity, out var xform))
            return;

        var station = _station.GetStationInMap(xform.MapID);

        if (station != null)
            args.Progress = CaptureProgress(_number.GetTarget(uid), station.Value);
    }
    private float CaptureProgress(int target, EntityUid station)
    {
        if (target == 0)
            return 1f;

        List<TileRef>? tileList = null;

        foreach (var kvp in TileRefs)
        {
            if (kvp.Key == station)
            {
                tileList = kvp.Value;
                break;
            }
        }

        if (tileList == null)
            return 0f;

        float progress = MathF.Min((float) tileList.Count / (float) target, 1f);

        return progress;
    }

    public bool IsContains(TileRef tile, EntityUid station)
    {
        // Проходим по словарю, чтобы найти список, связанный с указанным EntityUid
        foreach (var kvp in TileRefs)
        {
            if (kvp.Key == station)
            {
                // Проверяем, содержит ли список указанный TileRef
                return kvp.Value.Contains(tile);
            }
        }

        // Если подходящий список не был найден или TileRef в списке нет, возвращаем false
        return false;
    }

    public bool TryAddTile(TileRef tile, EntityUid station)
    {
        if (!TileRefs.TryGetValue(station, out var tileList))
        {
            // Если не существует, создаем новый список
            tileList = new List<TileRef>();
            TileRefs.Add(station, tileList);
        }

        foreach (var kvp in TileRefs)
        {
            if (kvp.Key == station)
            {
                // Проверяем, содержит ли список указанный TileRef
                if (kvp.Value.Contains(tile))
                {
                    return false;
                }
                else
                {
                    AddTile(tile, station);
                    return true;
                }
            }
        }

        return false;
    }
    private void AddTile(TileRef tile, EntityUid station)
    {
        // Перебираем все записи в словаре
        foreach (var kvp in TileRefs)
        {
            // Если находим список, связанный с нужным EntityUid
            if (kvp.Key == station)
            {
                // Добавляем TileRef в найденный список
                kvp.Value.Add(tile);
                return;
            }
        }
    }

    public void RemTile(TileRef tile, EntityUid station)
    {
        // Перебираем все записи в словаре
        foreach (var kvp in TileRefs)
        {
            // Если находим список, связанный с нужным EntityUid
            if (kvp.Key == station)
            {
                // Удаляем TileRef из списка
                kvp.Value.Remove(tile);

                // Если список пуст после удаления, можно удалить его из словаря
                if (kvp.Value.Count == 0)
                {
                    TileRefs.Remove(kvp.Key);
                }
                return;
            }
        }

        // Если подходящий список не был найден, ничего не происходит
    }

}
