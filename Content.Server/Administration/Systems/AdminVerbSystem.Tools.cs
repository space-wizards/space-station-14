using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Administration.Components;
using Content.Server.Cargo.Components;
using Content.Server.Doors.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Stacks;
using Content.Shared.Station.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly AdminTestArenaSystem _adminTestArenaSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    protected override void AddTricksVerbs(GetVerbsEvent<Verb> args)
    {
        base.AddTricksVerbs(args);

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (TryComp<BatteryComponent>(args.Target, out var battery))
        {
            Verb refillBattery = new()
            {
                Text = "Refill Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill_battery.png")),
                Act = () => _batterySystem.SetCharge(args.Target, battery.MaxCharge, battery),
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-refill-battery-description"),
                Priority = (int)TricksVerbPriorities.RefillBattery,
            };
            args.Verbs.Add(refillBattery);

            Verb drainBattery = new()
            {
                Text = "Drain Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/drain_battery.png")),
                Act = () => _batterySystem.SetCharge(args.Target, 0, battery),
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-drain-battery-description"),
                Priority = (int)TricksVerbPriorities.DrainBattery,
            };
            args.Verbs.Add(drainBattery);

            Verb infiniteBattery = new()
            {
                Text = "Infinite Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/infinite_battery.png")),
                Act = () =>
                {
                    var recharger = EnsureComp<BatterySelfRechargerComponent>(args.Target);
                    recharger.AutoRecharge = true;
                    recharger.AutoRechargeRate = battery.MaxCharge; // Instant refill.
                    recharger.AutoRechargePause = false; // No delay.
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-infinite-battery-object-description"),
                Priority = (int)TricksVerbPriorities.InfiniteBattery,
            };
            args.Verbs.Add(infiniteBattery);
        }

        if (TryGetGridChildren(args.Target, out var childEnum))
        {
            Verb refillBattery = new()
            {
                Text = "Refill Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill_battery.png")),
                Act = () =>
                {
                    foreach (var ent in childEnum)
                    {
                        if (!HasComp<StationInfiniteBatteryTargetComponent>(ent))
                            continue;
                        var battery = EnsureComp<BatteryComponent>(ent);
                        _batterySystem.SetCharge(ent, battery.MaxCharge, battery);
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-refill-battery-description"),
                Priority = (int) TricksVerbPriorities.RefillBattery,
            };
            args.Verbs.Add(refillBattery);

            Verb drainBattery = new()
            {
                Text = "Drain Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/drain_battery.png")),
                Act = () =>
                {
                    foreach (var ent in childEnum)
                    {
                        if (!HasComp<StationInfiniteBatteryTargetComponent>(ent))
                            continue;
                        var battery = EnsureComp<BatteryComponent>(ent);
                        _batterySystem.SetCharge(ent, 0, battery);
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-drain-battery-description"),
                Priority = (int) TricksVerbPriorities.DrainBattery,
            };
            args.Verbs.Add(drainBattery);

            Verb infiniteBattery = new()
            {
                Text = "Infinite Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/infinite_battery.png")),
                Act = () =>
                {
                    // this kills the sloth
                    foreach (var ent in childEnum)
                    {
                        if (!HasComp<StationInfiniteBatteryTargetComponent>(ent))
                            continue;

                        var recharger = EnsureComp<BatterySelfRechargerComponent>(ent);
                        var battery = EnsureComp<BatteryComponent>(ent);

                        recharger.AutoRecharge = true;
                        recharger.AutoRechargeRate = battery.MaxCharge; // Instant refill.
                        recharger.AutoRechargePause = false; // No delay.
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-infinite-battery-description"),
                Priority = (int) TricksVerbPriorities.InfiniteBattery,
            };
            args.Verbs.Add(infiniteBattery);
        }
    }

    protected override void ToolsSendToArenaVerb(ICommonSession player, EntityUid target)
    {
        var (mapUid, gridUid) = _adminTestArenaSystem.AssertArenaLoaded(player);
        _transformSystem.SetCoordinates(target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
    }

    protected override void ToolsAdjustStackVerb(ICommonSession player, EntityUid target, StackComponent stack)
    {
        // Unbounded intentionally.
        _quickDialog.OpenDialog(player,
            "Adjust stack",
            $"Amount (max {_stackSystem.GetMaxCount(stack)})",
            (int newAmount) =>
        {
            _stackSystem.SetCount(target, newAmount, stack);
        });
    }

    protected override void ToolsRenameVerb(ICommonSession player, EntityUid target)
    {
        _quickDialog.OpenDialog(player,
            "Rename",
            "Name",
            (string newName) =>
        {
            _metaSystem.SetEntityName(target, newName);
        });
    }

    protected override void ToolsRedescribeVerb(ICommonSession player, EntityUid target)
    {
        _quickDialog.OpenDialog(player,
            "Redescribe",
            "Description",
            (LongString newDescription) =>
        {
            _metaSystem.SetEntityDescription(target, newDescription.String);
        });
    }

    protected override void ToolsRenameAndRedescribeVerb(ICommonSession player, EntityUid target)
    {
        _quickDialog.OpenDialog(player,
            "Rename & Redescribe",
            "Name",
            "Description",
            (string newName, LongString newDescription) =>
            {
                var meta = MetaData(target);
                _metaSystem.SetEntityName(target, newName, meta);
                _metaSystem.SetEntityDescription(target, newDescription.String, meta);
            });
    }

    protected override void ToolsBarJobSlotsVerb(EntityUid target)
    {
        foreach (var (job, _) in _stationJobsSystem.GetJobs(target))
        {
            _stationJobsSystem.TrySetJobSlot(target, job, 0, true);
        }
    }

    protected override void ToolsLocateCargoShuttleVerb(EntityUid user, EntityUid target)
    {
        var shuttle = Comp<StationCargoOrderDatabaseComponent>(target).Shuttle;

        if (shuttle is null)
            return;

        _transformSystem.SetCoordinates(user, new EntityCoordinates(shuttle.Value, Vector2.Zero));
    }

    protected override void ToolsMakeMinigunVerb(EntityUid target, GunComponent gun)
    {
        EnsureComp<AdminMinigunComponent>(target);
        _gun.RefreshModifiers((target, gun));
    }

    protected override void ToolsSetBulletAmountVerb(ICommonSession player, EntityUid target, BallisticAmmoProviderComponent ballisticAmmo)
    {
        _quickDialog.OpenDialog(player,
            "Set Bullet Amount",
            $"Amount (standard {ballisticAmmo.Capacity}):",
            (string amount) =>
            {
                if (!int.TryParse(amount, out var result))
                    return;

                _gun.SetBallisticUnspawned((target, ballisticAmmo), result);
                _gun.UpdateBallisticAppearance(target, ballisticAmmo);
            });
    }

    private bool TryGetGridChildren(EntityUid target, [NotNullWhen(true)] out IEnumerable<EntityUid>? enumerator)
    {
        if (!HasComp<MapComponent>(target) && !HasComp<MapGridComponent>(target) &&
            !HasComp<StationDataComponent>(target))
        {
            enumerator = null;
            return false;
        }

        enumerator = GetGridChildrenInner(target);
        return true;
    }

    // ew. This finds everything supposedly on a grid.
    private IEnumerable<EntityUid> GetGridChildrenInner(EntityUid target)
    {
        if (TryComp<StationDataComponent>(target, out var station))
        {
            foreach (var grid in station.Grids)
            {
                var enumerator = Transform(grid).ChildEnumerator;
                while (enumerator.MoveNext(out var ent))
                {
                    yield return ent;
                }
            }
        }
        else if (HasComp<MapComponent>(target))
        {
            var enumerator = Transform(target).ChildEnumerator;
            while (enumerator.MoveNext(out var possibleGrid))
            {
                var enumerator2 = Transform(possibleGrid).ChildEnumerator;
                while (enumerator2.MoveNext(out var ent))
                {
                    yield return ent;
                }
            }
        }
        else
        {
            var enumerator = Transform(target).ChildEnumerator;
            while (enumerator.MoveNext(out var ent))
            {
                yield return ent;
            }
        }
    }
}
