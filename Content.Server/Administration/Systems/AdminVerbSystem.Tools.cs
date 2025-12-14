using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Components;
using Content.Server.Doors.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
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
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly AirlockSystem _airlockSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly AdminTestArenaSystem _adminTestArenaSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly JointSystem _jointSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly PredictedBatterySystem _predictedBatterySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    private void AddTricksVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (TryComp<DoorBoltComponent>(args.Target, out var bolts))
        {
            Verb bolt = new()
            {
                Text = Loc.GetString(bolts.BoltsDown ? "admin-verbs-unbolt" : "admin-verbs-bolt"),
                Category = VerbCategory.Tricks,
                Icon = bolts.BoltsDown
                    ? new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/unbolt.png"))
                    : new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/bolt.png")),
                Act = () =>
                {
                    _door.SetBoltsDown((args.Target, bolts), !bolts.BoltsDown);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString(bolts.BoltsDown
                    ? "admin-trick-unbolt-description"
                    : "admin-trick-bolt-description"),
                Priority = (int)(bolts.BoltsDown ? TricksVerbPriorities.Unbolt : TricksVerbPriorities.Bolt),
            };
            args.Verbs.Add(bolt);
        }

        if (TryComp<AirlockComponent>(args.Target, out var airlockComp))
        {
            Verb emergencyAccess = new()
            {
                Text = Loc.GetString(airlockComp.EmergencyAccess ? "admin-verbs-emergency-access-off" : "admin-verbs-emergency-access-on"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/emergency_access.png")),
                Act = () =>
                {
                    _airlockSystem.SetEmergencyAccess((args.Target, airlockComp), !airlockComp.EmergencyAccess);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString(airlockComp.EmergencyAccess
                    ? "admin-trick-emergency-access-off-description"
                    : "admin-trick-emergency-access-on-description"),
                Priority = (int)(airlockComp.EmergencyAccess ? TricksVerbPriorities.EmergencyAccessOff : TricksVerbPriorities.EmergencyAccessOn),
            };
            args.Verbs.Add(emergencyAccess);
        }

        if (HasComp<DamageableComponent>(args.Target))
        {
            Verb rejuvenate = new()
            {
                Text = Loc.GetString("admin-verbs-rejuvenate"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rejuvenate.png")),
                Act = () =>
                {
                    _rejuvenate.PerformRejuvenate(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-rejuvenate-description"),
                Priority = (int)TricksVerbPriorities.Rejuvenate,
            };
            args.Verbs.Add(rejuvenate);
        }

        if (!HasComp<GodmodeComponent>(args.Target))
        {
            Verb makeIndestructible = new()
            {
                Text = Loc.GetString("admin-verbs-make-indestructible"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
                Act = () =>
                {
                    _sharedGodmodeSystem.EnableGodmode(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-make-indestructible-description"),
                Priority = (int)TricksVerbPriorities.MakeIndestructible,
            };
            args.Verbs.Add(makeIndestructible);
        }
        else
        {
            Verb makeVulnerable = new()
            {
                Text = Loc.GetString("admin-verbs-make-vulnerable"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
                Act = () =>
                {
                    _sharedGodmodeSystem.DisableGodmode(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-make-vulnerable-description"),
                Priority = (int)TricksVerbPriorities.MakeVulnerable,
            };
            args.Verbs.Add(makeVulnerable);
        }

        if (TryComp<PredictedBatteryComponent>(args.Target, out var pBattery))
        {
            Verb refillBattery = new()
            {
                Text = Loc.GetString("admin-verbs-refill-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill_battery.png")),
                Act = () =>
                {
                    _predictedBatterySystem.SetCharge((args.Target, pBattery), pBattery.MaxCharge);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-refill-battery-description"),
                Priority = (int)TricksVerbPriorities.RefillBattery,
            };
            args.Verbs.Add(refillBattery);

            Verb drainBattery = new()
            {
                Text = Loc.GetString("admin-verbs-drain-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/drain_battery.png")),
                Act = () =>
                {
                    _predictedBatterySystem.SetCharge((args.Target, pBattery), 0);
                },
                Impact = LogImpact.Medium,
                Priority = (int)TricksVerbPriorities.DrainBattery,
            };
            args.Verbs.Add(drainBattery);

            Verb infiniteBattery = new()
            {
                Text = Loc.GetString("admin-verbs-infinite-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/infinite_battery.png")),
                Act = () =>
                {
                    var recharger = EnsureComp<PredictedBatterySelfRechargerComponent>(args.Target);
                    recharger.AutoRechargeRate = pBattery.MaxCharge; // Instant refill.
                    recharger.AutoRechargePauseTime = TimeSpan.Zero; // No delay.
                    Dirty(args.Target, recharger);
                    _predictedBatterySystem.RefreshChargeRate((args.Target, pBattery));
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-infinite-battery-object-description"),
                Priority = (int)TricksVerbPriorities.InfiniteBattery,
            };
            args.Verbs.Add(infiniteBattery);
        }

        if (TryComp<BatteryComponent>(args.Target, out var battery))
        {
            Verb refillBattery = new()
            {
                Text = Loc.GetString("admin-verbs-refill-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill_battery.png")),
                Act = () =>
                {
                    _batterySystem.SetCharge((args.Target, battery), battery.MaxCharge);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-refill-battery-description"),
                Priority = (int)TricksVerbPriorities.RefillBattery,
            };
            args.Verbs.Add(refillBattery);

            Verb drainBattery = new()
            {
                Text = Loc.GetString("admin-verbs-drain-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/drain_battery.png")),
                Act = () =>
                {
                    _batterySystem.SetCharge((args.Target, battery), 0);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-drain-battery-description"),
                Priority = (int)TricksVerbPriorities.DrainBattery,
            };
            args.Verbs.Add(drainBattery);

            Verb infiniteBattery = new()
            {
                Text = Loc.GetString("admin-verbs-infinite-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/infinite_battery.png")),
                Act = () =>
                {
                    var recharger = EnsureComp<BatterySelfRechargerComponent>(args.Target);
                    recharger.AutoRechargeRate = battery.MaxCharge; // Instant refill.
                    recharger.AutoRechargePauseTime = TimeSpan.Zero; // No delay.
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-infinite-battery-object-description"),
                Priority = (int)TricksVerbPriorities.InfiniteBattery,
            };
            args.Verbs.Add(infiniteBattery);
        }

        if (TryComp<AnchorableComponent>(args.Target, out var anchor))
        {
            Verb blockUnanchor = new()
            {
                Text = Loc.GetString("admin-verbs-block-unanchoring"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/anchor.svg.192dpi.png")),
                Act = () =>
                {
                    RemComp(args.Target, anchor);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-block-unanchoring-description"),
                Priority = (int)TricksVerbPriorities.BlockUnanchoring,
            };
            args.Verbs.Add(blockUnanchor);
        }

        if (TryComp<GasTankComponent>(args.Target, out var tank))
        {
            Verb refillInternalsO2 = new()
            {
                Text = Loc.GetString("admin-verbs-refill-internals-oxygen"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Tanks/oxygen.rsi"), "icon"),
                Act = () =>
                {
                    RefillGasTank(args.Target, Gas.Oxygen, tank);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-internals-refill-oxygen-description"),
                Priority = (int)TricksVerbPriorities.RefillOxygen,
            };
            args.Verbs.Add(refillInternalsO2);

            Verb refillInternalsN2 = new()
            {
                Text = Loc.GetString("admin-verbs-refill-internals-nitrogen"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Tanks/red.rsi"), "icon"),
                Act = () =>
                {
                    RefillGasTank(args.Target, Gas.Nitrogen, tank);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-internals-refill-nitrogen-description"),
                Priority = (int)TricksVerbPriorities.RefillNitrogen,
            };
            args.Verbs.Add(refillInternalsN2);

            Verb refillInternalsPlasma = new()
            {
                Text = Loc.GetString("admin-verbs-refill-internals-plasma"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Tanks/plasma.rsi"), "icon"),
                Act = () =>
                {
                    RefillGasTank(args.Target, Gas.Plasma, tank);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-internals-refill-plasma-description"),
                Priority = (int)TricksVerbPriorities.RefillPlasma,
            };
            args.Verbs.Add(refillInternalsPlasma);
        }

        if (HasComp<InventoryComponent>(args.Target))
        {
            Verb refillInternalsO2 = new()
            {
                Text = Loc.GetString("admin-verbs-refill-internals-oxygen"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Tanks/oxygen.rsi"), "icon"),
                Act = () => RefillEquippedTanks(args.User, Gas.Oxygen),
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-internals-refill-oxygen-description"),
                Priority = (int)TricksVerbPriorities.RefillOxygen,
            };
            args.Verbs.Add(refillInternalsO2);

            Verb refillInternalsN2 = new()
            {
                Text = Loc.GetString("admin-verbs-refill-internals-nitrogen"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Tanks/red.rsi"), "icon"),
                Act = () => RefillEquippedTanks(args.User, Gas.Nitrogen),
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-internals-refill-nitrogen-description"),
                Priority = (int)TricksVerbPriorities.RefillNitrogen,
            };
            args.Verbs.Add(refillInternalsN2);

            Verb refillInternalsPlasma = new()
            {
                Text = Loc.GetString("admin-verbs-refill-internals-plasma"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Tanks/plasma.rsi"), "icon"),
                Act = () => RefillEquippedTanks(args.User, Gas.Plasma),
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-internals-refill-plasma-description"),
                Priority = (int)TricksVerbPriorities.RefillPlasma,
            };
            args.Verbs.Add(refillInternalsPlasma);
        }

        Verb sendToTestArena = new()
        {
            Text = Loc.GetString("admin-verbs-send-to-test-arena"),
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaSystem.AssertArenaLoaded(player);
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-description"),
            Priority = (int)TricksVerbPriorities.SendToTestArena,
        };
        args.Verbs.Add(sendToTestArena);

        var activeId = FindActiveId(args.Target);

        if (activeId is not null)
        {
            Verb grantAllAccess = new()
            {
                Text = Loc.GetString("admin-verbs-grant-all-access"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/id_cards.rsi"), "centcom"),
                Act = () =>
                {
                    GiveAllAccess(activeId.Value);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-grant-all-access-description"),
                Priority = (int)TricksVerbPriorities.GrantAllAccess,
            };
            args.Verbs.Add(grantAllAccess);

            Verb revokeAllAccess = new()
            {
                Text = Loc.GetString("admin-verbs-revoke-all-access"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/id_cards.rsi"), "default"),
                Act = () =>
                {
                    RevokeAllAccess(activeId.Value);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-revoke-all-access-description"),
                Priority = (int)TricksVerbPriorities.RevokeAllAccess,
            };
            args.Verbs.Add(revokeAllAccess);
        }

        if (HasComp<AccessComponent>(args.Target))
        {
            Verb grantAllAccess = new()
            {
                Text = Loc.GetString("admin-verbs-grant-all-access"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/id_cards.rsi"), "centcom"),
                Act = () =>
                {
                    GiveAllAccess(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-grant-all-access-description"),
                Priority = (int)TricksVerbPriorities.GrantAllAccess,
            };
            args.Verbs.Add(grantAllAccess);

            Verb revokeAllAccess = new()
            {
                Text = Loc.GetString("admin-verbs-revoke-all-access"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/id_cards.rsi"), "default"),
                Act = () =>
                {
                    RevokeAllAccess(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-revoke-all-access-description"),
                Priority = (int)TricksVerbPriorities.RevokeAllAccess,
            };
            args.Verbs.Add(revokeAllAccess);
        }

        if (TryComp<StackComponent>(args.Target, out var stack))
        {
            Verb adjustStack = new()
            {
                Text = Loc.GetString("admin-verbs-adjust-stack"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/adjust-stack.png")),
                Act = () =>
                {
                    // Unbounded intentionally.
                    _quickDialog.OpenDialog(player, Loc.GetString("admin-verbs-adjust-stack"), Loc.GetString("admin-verbs-dialog-adjust-stack-amount", ("max", _stackSystem.GetMaxCount(stack))), (int newAmount) =>
                    {
                        _stackSystem.SetCount((args.Target, stack), newAmount);
                    });
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-adjust-stack-description"),
                Priority = (int) TricksVerbPriorities.AdjustStack,
            };
            args.Verbs.Add(adjustStack);

            Verb fillStack = new()
            {
                Text = Loc.GetString("admin-verbs-fill-stack"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill-stack.png")),
                Act = () =>
                {
                    _stackSystem.SetCount((args.Target, stack), _stackSystem.GetMaxCount(stack));
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-fill-stack-description"),
                Priority = (int) TricksVerbPriorities.FillStack,
            };
            args.Verbs.Add(fillStack);
        }

        Verb rename = new()
        {
            Text = Loc.GetString("admin-verbs-rename"),
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, Loc.GetString("admin-verbs-dialog-rename-title"), Loc.GetString("admin-verbs-dialog-rename-name"), (string newName) =>
                {
                    _metaSystem.SetEntityName(args.Target, newName);
                });
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-description"),
            Priority = (int) TricksVerbPriorities.Rename,
        };
        args.Verbs.Add(rename);

        Verb redescribe = new()
        {
            Text = Loc.GetString("admin-verbs-redescribe"),
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/redescribe.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, Loc.GetString("admin-verbs-dialog-redescribe-title"), Loc.GetString("admin-verbs-dialog-redescribe-description"), (LongString newDescription) =>
                {
                    _metaSystem.SetEntityDescription(args.Target, newDescription.String);
                });
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-redescribe-description"),
            Priority = (int) TricksVerbPriorities.Redescribe,
        };
        args.Verbs.Add(redescribe);

        Verb renameAndRedescribe = new()
        {
            Text = Loc.GetString("admin-verbs-rename-and-redescribe"),
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename_and_redescribe.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, Loc.GetString("admin-verbs-dialog-rename-and-redescribe-title"), Loc.GetString("admin-verbs-dialog-rename-name"), Loc.GetString("admin-verbs-dialog-redescribe-description"),
                    (string newName, LongString newDescription) =>
                    {
                        var meta = MetaData(args.Target);
                        _metaSystem.SetEntityName(args.Target, newName, meta);
                        _metaSystem.SetEntityDescription(args.Target, newDescription.String, meta);
                    });
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-and-redescribe-description"),
            Priority = (int) TricksVerbPriorities.RenameAndRedescribe,
        };
        args.Verbs.Add(renameAndRedescribe);

        if (TryComp<StationDataComponent>(args.Target, out var stationData))
        {
            if (_adminManager.HasAdminFlag(player, AdminFlags.Round))
            {
                Verb barJobSlots = new()
                {
                    Text = Loc.GetString("admin-verbs-bar-job-slots"),
                    Category = VerbCategory.Tricks,
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/bar_jobslots.png")),
                    Act = () =>
                    {
                        foreach (var (job, _) in _stationJobsSystem.GetJobs(args.Target))
                        {
                            _stationJobsSystem.TrySetJobSlot(args.Target, job, 0, true);
                        }
                    },
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-bar-job-slots-description"),
                    Priority = (int) TricksVerbPriorities.BarJobSlots,
                };
                args.Verbs.Add(barJobSlots);
            }

            Verb locateCargoShuttle = new()
            {
                Text = Loc.GetString("admin-verbs-locate-cargo-shuttle"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Soft/cargosoft.rsi"), "icon"),
                Act = () =>
                {
                    var shuttle = Comp<StationCargoOrderDatabaseComponent>(args.Target).Shuttle;

                    if (shuttle is null)
                        return;

                    _transformSystem.SetCoordinates(args.User, new EntityCoordinates(shuttle.Value, Vector2.Zero));
                },
                Impact = LogImpact.Low,
                Message = Loc.GetString("admin-trick-locate-cargo-shuttle-description"),
                Priority = (int) TricksVerbPriorities.LocateCargoShuttle,
            };
            args.Verbs.Add(locateCargoShuttle);
        }

        if (TryGetGridChildren(args.Target, out var childEnum))
        {
            Verb refillBattery = new()
            {
                Text = Loc.GetString("admin-verbs-refill-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill_battery.png")),
                Act = () =>
                {
                    foreach (var ent in childEnum)
                    {
                        if (!HasComp<StationInfiniteBatteryTargetComponent>(ent))
                            continue;
                        var battery = EnsureComp<BatteryComponent>(ent);
                        _batterySystem.SetCharge((ent, battery), battery.MaxCharge);
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-refill-battery-description"),
                Priority = (int) TricksVerbPriorities.RefillBattery,
            };
            args.Verbs.Add(refillBattery);

            Verb drainBattery = new()
            {
                Text = Loc.GetString("admin-verbs-drain-battery"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/drain_battery.png")),
                Act = () =>
                {
                    foreach (var ent in childEnum)
                    {
                        if (!HasComp<StationInfiniteBatteryTargetComponent>(ent))
                            continue;
                        var battery = EnsureComp<BatteryComponent>(ent);
                        _batterySystem.SetCharge((ent, battery), 0);
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-drain-battery-description"),
                Priority = (int) TricksVerbPriorities.DrainBattery,
            };
            args.Verbs.Add(drainBattery);

            Verb infiniteBattery = new()
            {
                Text = Loc.GetString("admin-verbs-infinite-battery"),
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

                        recharger.AutoRechargeRate = battery.MaxCharge; // Instant refill.
                        recharger.AutoRechargePauseTime = TimeSpan.Zero; // No delay.
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-infinite-battery-description"),
                Priority = (int) TricksVerbPriorities.InfiniteBattery,
            };
            args.Verbs.Add(infiniteBattery);
        }

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            Verb haltMovement = new()
            {
                Text = Loc.GetString("admin-verbs-halt-movement"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/halt.png")),
                Act = () =>
                {
                    _physics.SetLinearVelocity(args.Target, Vector2.Zero, body: physics);
                    _physics.SetAngularVelocity(args.Target, 0f, body: physics);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-halt-movement-description"),
                Priority = (int) TricksVerbPriorities.HaltMovement,
            };
            args.Verbs.Add(haltMovement);
        }

        if (TryComp<MapComponent>(args.Target, out var map))
        {
            if (_adminManager.HasAdminFlag(player, AdminFlags.Mapping))
            {
                if (_map.IsPaused(map.MapId))
                {
                    Verb unpauseMap = new()
                    {
                        Text = Loc.GetString("admin-verbs-unpause-map"),
                        Category = VerbCategory.Tricks,
                        Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/play.png")),
                        Act = () =>
                        {
                            _map.SetPaused(map.MapId, false);
                        },
                        Impact = LogImpact.Extreme,
                        Message = Loc.GetString("admin-trick-unpause-map-description"),
                        Priority = (int) TricksVerbPriorities.Unpause,
                    };
                    args.Verbs.Add(unpauseMap);
                }
                else
                {
                    Verb pauseMap = new()
                    {
                        Text = Loc.GetString("admin-verbs-pause-map"),
                        Category = VerbCategory.Tricks,
                        Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/pause.png")),
                        Act = () =>
                        {
                            _map.SetPaused(map.MapId, true);
                        },
                        Impact = LogImpact.Extreme,
                        Message = Loc.GetString("admin-trick-pause-map-description"),
                        Priority = (int) TricksVerbPriorities.Pause,
                    };
                    args.Verbs.Add(pauseMap);
                }
            }
        }

        if (TryComp<JointComponent>(args.Target, out var joints))
        {
            Verb snapJoints = new()
            {
                Text = Loc.GetString("admin-verbs-snap-joints"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/snap_joints.png")),
                Act = () =>
                {
                    _jointSystem.ClearJoints(args.Target, joints);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-snap-joints-description"),
                Priority = (int) TricksVerbPriorities.SnapJoints,
            };
            args.Verbs.Add(snapJoints);
        }

        if (TryComp<GunComponent>(args.Target, out var gun))
        {
            Verb minigunFire = new()
            {
                Text = Loc.GetString("admin-verbs-make-minigun"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Weapons/Guns/HMGs/minigun.rsi"), "icon"),
                Act = () =>
                {
                    EnsureComp<AdminMinigunComponent>(args.Target);
                    _gun.RefreshModifiers((args.Target, gun));
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-minigun-fire-description"),
                Priority = (int) TricksVerbPriorities.MakeMinigun,
            };
            args.Verbs.Add(minigunFire);
        }

        if (TryComp<BallisticAmmoProviderComponent>(args.Target, out var ballisticAmmo))
        {
            Verb setCapacity = new()
            {
                Text = Loc.GetString("admin-verbs-set-bullet-amount"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Fun/caps.rsi"), "mag-6"),
                Act = () =>
                {
                    _quickDialog.OpenDialog(player, Loc.GetString("admin-verbs-dialog-set-bullet-amount-title"), Loc.GetString("admin-verbs-dialog-set-bullet-amount-amount", ("cap", ballisticAmmo.Capacity)), (string amount) =>
                    {
                        if (!int.TryParse(amount, out var result))
                            return;

                        _gun.SetBallisticUnspawned((args.Target, ballisticAmmo), result);
                        _gun.UpdateBallisticAppearance(args.Target, ballisticAmmo);
                    });
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-set-bullet-amount-description"),
                Priority = (int) TricksVerbPriorities.SetBulletAmount,
            };
            args.Verbs.Add(setCapacity);
        }
    }

    private void RefillEquippedTanks(EntityUid target, Gas gasType)
    {
        foreach (var held in _inventorySystem.GetHandOrInventoryEntities(target))
        {
            RefillGasTank(held, gasType);
        }
    }

    private void RefillGasTank(EntityUid tank, Gas gasType, GasTankComponent? tankComponent = null)
    {
        if (!Resolve(tank, ref tankComponent, false))
            return;

        var mixSize = tankComponent.Air.Volume;
        var newMix = new GasMixture(mixSize);
        newMix.SetMoles(gasType, (1000.0f * mixSize) / (Atmospherics.R * Atmospherics.T20C)); // Fill the tank to 1000KPA.
        newMix.Temperature = Atmospherics.T20C;
        tankComponent.Air = newMix;
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

    private EntityUid? FindActiveId(EntityUid target)
    {
        if (_inventorySystem.TryGetSlotEntity(target, "id", out var slotEntity))
        {
            if (HasComp<AccessComponent>(slotEntity))
            {
                return slotEntity.Value;
            }
            else if (TryComp<PdaComponent>(slotEntity, out var pda)
                && HasComp<IdCardComponent>(pda.ContainedId))
            {
                return pda.ContainedId;
            }
        }
        else if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var held in _handsSystem.EnumerateHeld((target, hands)))
            {
                if (HasComp<AccessComponent>(held))
                {
                    return held;
                }
            }
        }

        return null;
    }

    private void GiveAllAccess(EntityUid entity)
    {
        var allAccess = _prototypeManager
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => new ProtoId<AccessLevelPrototype>(p.ID)).ToArray();

        _accessSystem.TrySetTags(entity, allAccess);
    }

    private void RevokeAllAccess(EntityUid entity)
    {
        _accessSystem.TrySetTags(entity, new List<ProtoId<AccessLevelPrototype>>());
    }

    public enum TricksVerbPriorities
    {
        Bolt = 0,
        Unbolt = -1,
        EmergencyAccessOn = -2,
        EmergencyAccessOff = -3,
        MakeIndestructible = -4,
        MakeVulnerable = -5,
        BlockUnanchoring = -6,
        RefillBattery = -7,
        DrainBattery = -8,
        RefillOxygen = -9,
        RefillNitrogen = -10,
        RefillPlasma = -11,
        SendToTestArena = -12,
        GrantAllAccess = -13,
        RevokeAllAccess = -14,
        Rejuvenate = -15,
        AdjustStack = -16,
        FillStack = -17,
        Rename = -18,
        Redescribe = -19,
        RenameAndRedescribe = -20,
        BarJobSlots = -21,
        LocateCargoShuttle = -22,
        InfiniteBattery = -23,
        HaltMovement = -24,
        Unpause = -25,
        Pause = -26,
        SnapJoints = -27,
        MakeMinigun = -28,
        SetBulletAmount = -29,
    }
}
