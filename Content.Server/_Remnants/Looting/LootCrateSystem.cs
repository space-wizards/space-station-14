using Content.Shared._Remnants.Looting;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server._Remnants.Looting;

public sealed class LootCrateSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly HashSet<EntityUid> _activeInteractions = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<LootCrateComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<LootCrateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInteract(EntityUid uid, LootCrateComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_activeInteractions.Add(uid))
            return;

        try
        {
            var now = _timing.CurTime;
            var name = Name(uid);

            if (now < component.NextUseTime)
            {
                var remaining = component.NextUseTime - now;

                _popup.PopupEntity( $"The {name} is cooling down. {Math.Ceiling(remaining.TotalSeconds)}s remaining.", uid, args.User);

                args.Handled = true;
                return;
            }
            args.Handled = true;

            _popup.PopupEntity($"You open the {name}!", uid, args.User);

            var roll = _random.NextFloat() * 100f;

            EntProtoId? selectedTable;

            if (roll < 1f)
                selectedTable = component.LegendaryLootTable;
            else if (roll < 10f)
                selectedTable = component.RareLootTable;
            else
                selectedTable = component.CommonLootTable;

            if (selectedTable == null)
                return;

            var tableUid = _entMan.SpawnEntity(selectedTable, MapCoordinates.Nullspace);

            if (!_entMan.TryGetComponent<LootTableComponent>(tableUid, out var lootTable))
            {
                QueueDel(tableUid);
                return;
            }

            if (lootTable.Entries.Count == 0)
            {
                QueueDel(tableUid);
                return;
            }

            var selectedItem = _random.Pick(lootTable.Entries);

            QueueDel(tableUid);

            var coords = Transform(uid).Coordinates;
            var spawned = _entMan.SpawnEntity(selectedItem, coords);

            EnsureComp<FoundInRaidComponent>(spawned);

            var cooldown = _random.Next((int)component.CooldownMin.TotalSeconds, (int)component.CooldownMax.TotalSeconds);

            component.NextUseTime = now + TimeSpan.FromSeconds(cooldown);
        }
        finally
        {
            _activeInteractions.Remove(uid);
        }
    }

    private void OnExamined(EntityUid uid, LootCrateComponent component, ExaminedEvent args)
    {
        var now = _timing.CurTime;

        var name = Name(uid);

        if (now < component.NextUseTime)
        {
            var remaining = component.NextUseTime - now;

            args.PushMarkup($"The {name} is on cooldown. It can be opened again in {Math.Ceiling(remaining.TotalSeconds)} seconds.");
        }
        else
        {
            args.PushMarkup($"The {name} is ready to be opened!");
        }
    }
}
