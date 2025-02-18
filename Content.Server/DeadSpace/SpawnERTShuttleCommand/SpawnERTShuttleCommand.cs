// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Administration;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Content.Server.RoundEnd;
using Robust.Shared.Map.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server.DeadSpace.SpawnERTShuttleCommand;

[AdminCommand(AdminFlags.Spawn)]
public sealed class SpawnERTShuttleCommand : LocalizedCommands
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "ert_spawn_shuttle";
    public override string Description => "Создаёт и стыкует к станции ЦК шаттл приоритетом стыковочный порт ОБР.";
    public override string Help => "spawn_ert_shuttle <шаттл>";

    [ValidatePrototypeId<TagPrototype>]
    private const string DockTag = "DockCentcommERT";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var roundEnd = _entityManager.System<RoundEndSystem>();

        var centcommMap = roundEnd.GetCentcomm();
        var centcommGrid = roundEnd.GetCentcommGridEntity();

        if (!_entityManager.TryGetComponent(centcommMap, out MapComponent? mapComponent))
        {
            shell.WriteError("Ошибка: Не найден MapComponent у карты ЦК.");
            return;
        }

        if (!_prototypeManager.TryIndex(args[0], out ERTShuttlePrototype? shuttlePrototype))
        {
            shell.WriteError("Ошибка: Неверный аргумент.");
            return;
        }

        _entityManager.System<MapLoaderSystem>().TryLoadGrid(mapComponent.MapId, shuttlePrototype.Path, out var shuttle);

        if (_entityManager.Deleted(shuttle))
        {
            shell.WriteError("Ошибка: Шаттл не существует или был удалён.");
            return;
        }

        if (!_entityManager.TryGetComponent(shuttle, out ShuttleComponent? shuttleComponent))
        {
            shell.WriteError("Ошибка: Не найден ShuttleComponent у заспавненного шаттла.");
            return;
        }

        if (_entityManager.Deleted(centcommGrid))
        {
            shell.WriteError("Ошибка: ЦК не существует или было удалено.");
            return;
        }

        if (!_entityManager.System<ShuttleSystem>().TryFTLDock(shuttle.Value, shuttleComponent, centcommGrid.Value, out _, DockTag))
        {
            shell.WriteError("Ошибка: Стыковка не выполнена.");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var shuttles = _prototypeManager.EnumeratePrototypes<ERTShuttlePrototype>()
                .Select(p => new CompletionOption(p.ID));

            return CompletionResult.FromHintOptions(shuttles, "<шаттл>");
        }
        return CompletionResult.Empty;
    }
}
