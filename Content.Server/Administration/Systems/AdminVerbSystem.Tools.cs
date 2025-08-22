using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server._Starlight.Medical.Limbs;
using Content.Server.Administration.Components;
using Content.Server.Cargo.Components;
using Content.Server.Doors.Systems;
using Content.Server.Hands.Systems;
using Content.Server._Impstation.Thaven;
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
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Construction.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Hands.Components;
using Content.Shared._Impstation.Thaven.Components;
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
using Content.Shared.Overlays; // 🌟Starlight🌟
using Content.Shared.Contraband;
using Content.Shared.Humanoid; // 🌟Starlight🌟

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly AirlockSystem _airlockSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly AdminTestArenaSystem _adminTestArenaSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly JointSystem _jointSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    #region Starlight
    [Dependency] private readonly LimbSystem _limbSystem = default!;
    [Dependency] private readonly StarlightEntitySystem _entitySystem = default!;
    #endregion

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
                Text = bolts.BoltsDown ? "Unbolt" : "Bolt",
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

        if (TryComp<ElectrifiedComponent>(args.Target, out var electrified) && HasComp<DoorComponent>(args.Target))
        {
            Verb electrify = new()
            {
                Text = electrified.Enabled ? "Unelectrify" : "Electrify",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
                Act = () =>
                {
                    _electrocution.SetElectrified((args.Target, electrified), !electrified.Enabled);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString(electrified.Enabled
                    ? "admin-trick-unelectrify-description"
                    : "admin-trick-electrify-description"),
                Priority = (int)(electrified.Enabled ? TricksVerbPriorities.Unelectrify : TricksVerbPriorities.Electrify),
            };
            args.Verbs.Add(electrify);
        }


        if (TryComp<AirlockComponent>(args.Target, out var airlockComp))
        {
            Verb emergencyAccess = new()
            {
                Text = airlockComp.EmergencyAccess ? "Emergency Access Off" : "Emergency Access On",
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
                Text = "Rejuvenate",
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
                Text = "Make Indestructible",
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
                Text = "Make Vulnerable",
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

        if (TryComp<BatteryComponent>(args.Target, out var battery))
        {
            Verb refillBattery = new()
            {
                Text = "Refill Battery",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill_battery.png")),
                Act = () =>
                {
                    _batterySystem.SetCharge(args.Target, battery.MaxCharge, battery);
                },
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
                Act = () =>
                {
                    _batterySystem.SetCharge(args.Target, 0, battery);
                },
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

        if (TryComp<AnchorableComponent>(args.Target, out var anchor))
        {
            Verb blockUnanchor = new()
            {
                Text = "Block Unanchoring",
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
                Text = "Refill Internals Oxygen",
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
                Text = "Refill Internals Nitrogen",
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
                Text = "Refill Internals Plasma",
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
                Text = "Refill Internals Oxygen",
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
                Text = "Refill Internals Nitrogen",
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
                Text = "Refill Internals Plasma",
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
            Text = "Send to test arena",
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
                Text = "Grant All Access",
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
                Text = "Revoke All Access",
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
                Text = "Grant All Access",
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
                Text = "Revoke All Access",
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
                Text = "Adjust Stack",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/adjust-stack.png")),
                Act = () =>
                {
                    // Unbounded intentionally.
                    _quickDialog.OpenDialog(player, "Adjust stack", $"Amount (max {_stackSystem.GetMaxCount(stack)})", (int newAmount) =>
                    {
                        _stackSystem.SetCount(args.Target, newAmount, stack);
                    });
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-adjust-stack-description"),
                Priority = (int)TricksVerbPriorities.AdjustStack,
            };
            args.Verbs.Add(adjustStack);

            Verb fillStack = new()
            {
                Text = "Fill Stack",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill-stack.png")),
                Act = () =>
                {
                    _stackSystem.SetCount(args.Target, _stackSystem.GetMaxCount(stack), stack);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-fill-stack-description"),
                Priority = (int)TricksVerbPriorities.FillStack,
            };
            args.Verbs.Add(fillStack);
        }

        Verb rename = new()
        {
            Text = "Rename",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, "Rename", "Name", (string newName) =>
                {
                    _metaSystem.SetEntityName(args.Target, newName);
                });
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-description"),
            Priority = (int)TricksVerbPriorities.Rename,
        };
        args.Verbs.Add(rename);

        Verb redescribe = new()
        {
            Text = "Redescribe",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/redescribe.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, "Redescribe", "Description", (LongString newDescription) =>
                {
                    _metaSystem.SetEntityDescription(args.Target, newDescription.String);
                });
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-redescribe-description"),
            Priority = (int)TricksVerbPriorities.Redescribe,
        };
        args.Verbs.Add(redescribe);

        Verb renameAndRedescribe = new()
        {
            Text = "Redescribe",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename_and_redescribe.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, "Rename & Redescribe", "Name", "Description",
                    (string newName, LongString newDescription) =>
                    {
                        var meta = MetaData(args.Target);
                        _metaSystem.SetEntityName(args.Target, newName, meta);
                        _metaSystem.SetEntityDescription(args.Target, newDescription.String, meta);
                    });
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-and-redescribe-description"),
            Priority = (int)TricksVerbPriorities.RenameAndRedescribe,
        };
        args.Verbs.Add(renameAndRedescribe);

        if (TryComp<StationDataComponent>(args.Target, out var stationData))
        {
            if (_adminManager.HasAdminFlag(player, AdminFlags.Round))
            {
                Verb barJobSlots = new()
                {
                    Text = "Bar job slots",
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
                    Priority = (int)TricksVerbPriorities.BarJobSlots,
                };
                args.Verbs.Add(barJobSlots);
            }

            Verb locateCargoShuttle = new()
            {
                Text = "Locate Cargo Shuttle",
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
                Priority = (int)TricksVerbPriorities.LocateCargoShuttle,
            };
            args.Verbs.Add(locateCargoShuttle);
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
                Priority = (int)TricksVerbPriorities.RefillBattery,
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
                Priority = (int)TricksVerbPriorities.InfiniteBattery,
            };
            args.Verbs.Add(infiniteBattery);
        }

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            Verb haltMovement = new()
            {
                Text = "Halt Movement",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/halt.png")),
                Act = () =>
                {
                    _physics.SetLinearVelocity(args.Target, Vector2.Zero, body: physics);
                    _physics.SetAngularVelocity(args.Target, 0f, body: physics);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-halt-movement-description"),
                Priority = (int)TricksVerbPriorities.HaltMovement,
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
                        Text = "Unpause Map",
                        Category = VerbCategory.Tricks,
                        Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/play.png")),
                        Act = () =>
                        {
                            _map.SetPaused(map.MapId, false);
                        },
                        Impact = LogImpact.Extreme,
                        Message = Loc.GetString("admin-trick-unpause-map-description"),
                        Priority = (int)TricksVerbPriorities.Unpause,
                    };
                    args.Verbs.Add(unpauseMap);
                }
                else
                {
                    Verb pauseMap = new()
                    {
                        Text = "Pause Map",
                        Category = VerbCategory.Tricks,
                        Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/pause.png")),
                        Act = () =>
                        {
                            _map.SetPaused(map.MapId, true);
                        },
                        Impact = LogImpact.Extreme,
                        Message = Loc.GetString("admin-trick-pause-map-description"),
                        Priority = (int)TricksVerbPriorities.Pause,
                    };
                    args.Verbs.Add(pauseMap);
                }
            }
        }

        if (TryComp<JointComponent>(args.Target, out var joints))
        {
            Verb snapJoints = new()
            {
                Text = "Snap Joints",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/snap_joints.png")),
                Act = () =>
                {
                    _jointSystem.ClearJoints(args.Target, joints);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-snap-joints-description"),
                Priority = (int)TricksVerbPriorities.SnapJoints,
            };
            args.Verbs.Add(snapJoints);
        }

        if (TryComp<GunComponent>(args.Target, out var gun))
        {
            Verb minigunFire = new()
            {
                Text = "Make Minigun",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Weapons/Guns/HMGs/minigun.rsi"), "icon"),
                Act = () =>
                {
                    EnsureComp<AdminMinigunComponent>(args.Target);
                    _gun.RefreshModifiers((args.Target, gun));
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-minigun-fire-description"),
                Priority = (int)TricksVerbPriorities.MakeMinigun,
            };
            args.Verbs.Add(minigunFire);
        }

        if (TryComp<BallisticAmmoProviderComponent>(args.Target, out var ballisticAmmo))
        {
            Verb setCapacity = new()
            {
                Text = "Set Bullet Amount",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Fun/caps.rsi"), "mag-6"),
                Act = () =>
                {
                    _quickDialog.OpenDialog(player, "Set Bullet Amount", $"Amount (standard {ballisticAmmo.Capacity}):", (string amount) =>
                    {
                        if (!int.TryParse(amount, out var result))
                            return;

                        _gun.SetBallisticUnspawned((args.Target, ballisticAmmo), result);
                        _gun.UpdateBallisticAppearance(args.Target, ballisticAmmo);
                    });
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-set-bullet-amount-description"),
                Priority = (int)TricksVerbPriorities.SetBulletAmount,
            };
            args.Verbs.Add(setCapacity);
        }

        #region Starlight 
        // Add toggle overlays verb
        Verb toggleOverlays = new()
        {
            Text = "Toggle All Overlays",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/_Starlight/Interface/AdminActions/ToggleOverlays.png")),
            Act = () =>
            {
                // List of overlay components to toggle
                var overlaysPresent = false;
                overlaysPresent |= TryComp<ShowHealthBarsComponent>(args.Target, out _);
                overlaysPresent |= TryComp<ShowHealthIconsComponent>(args.Target, out _);
                overlaysPresent |= TryComp<ShowJobIconsComponent>(args.Target, out _);
                overlaysPresent |= TryComp<ShowMindShieldIconsComponent>(args.Target, out _);
                overlaysPresent |= TryComp<ShowSyndicateIconsComponent>(args.Target, out _);
                overlaysPresent |= TryComp<ShowCriminalRecordIconsComponent>(args.Target, out _);
                overlaysPresent |= TryComp<ShowContrabandDetailsComponent>(args.Target, out _);

                if (overlaysPresent)
                {
                    RemComp<ShowHealthBarsComponent>(args.Target);
                    RemComp<ShowHealthIconsComponent>(args.Target);
                    RemComp<ShowJobIconsComponent>(args.Target);
                    RemComp<ShowMindShieldIconsComponent>(args.Target);
                    RemComp<ShowSyndicateIconsComponent>(args.Target);
                    RemComp<ShowCriminalRecordIconsComponent>(args.Target);
                    RemComp<ShowContrabandDetailsComponent>(args.Target);
                }
                else
                {
                    var showHealthBars = EnsureComp<ShowHealthBarsComponent>(args.Target);
                    showHealthBars.DamageContainers.Add("Biological");
                    showHealthBars.HealthStatusIcon = "HealthIcon";

                    var showHealthIcons = EnsureComp<ShowHealthIconsComponent>(args.Target);
                    showHealthIcons.DamageContainers.Add("Biological");

                    EnsureComp<ShowJobIconsComponent>(args.Target);
                    EnsureComp<ShowMindShieldIconsComponent>(args.Target);
                    EnsureComp<ShowSyndicateIconsComponent>(args.Target);
                    EnsureComp<ShowCriminalRecordIconsComponent>(args.Target);
                    EnsureComp<ShowContrabandDetailsComponent>(args.Target);
                }
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-toggle-overlays-description"),
            Priority = (int)TricksVerbPriorities.ToggleOverlays,
        };
        args.Verbs.Add(toggleOverlays);

        // Reaper arm verb
        if (TryComp<BodyComponent>(args.Target, out var bodyComp))
        {
            Verb reaperArm = new()
            {
                Text = "Replace the right hand with a Reaper arm.",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/_Starlight/Mobs/Species/Cyberlimbs/parts.rsi"), "r_silver_arm"),
                Act = () =>
                {
                    var torso = _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Torso).FirstOrDefault();
                    var rightArm = _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Arm).FirstOrDefault(part => part.Component.Symmetry == BodyPartSymmetry.Right);
                    if (torso == default || rightArm == default)
                        return;

                    if (_entitySystem.TryEntity<TransformComponent, HumanoidAppearanceComponent, BodyComponent>(args.Target, out var body)
                    && _entitySystem.TryEntity<TransformComponent, MetaDataComponent, BodyPartComponent>(rightArm.Id, out var partEnt))
                    {
                        _limbSystem.Amputatate(body, partEnt);
                        var reaper = Spawn("RightArmCyberReaper", body.Comp1.Coordinates);
                        if (_entitySystem.TryEntity<BodyPartComponent>(reaper, out var reaperEnt))
                            _limbSystem.AttachLimb((body.Owner, body.Comp2), "right arm", torso, reaperEnt);
                    }
                },
                Impact = LogImpact.Medium,
                Message = "Replace the right hand with a Reaper arm.",
                Priority = (int)TricksVerbPriorities.SetBulletAmount,
            };
            args.Verbs.Add(reaperArm);
        }


        if (TryComp<ThavenMoodsComponent>(args.Target, out var moods))
        {
            Verb addRandomMood = new()
            {
                Text = "Add Random Mood",
                Category = VerbCategory.Tricks,
                // TODO: Icon
                Act = () =>
                {
                    _moods.TryAddRandomMood(args.Target);
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("admin-trick-add-random-mood-description"),
                Priority = (int)TricksVerbPriorities.AddRandomMood,
            };
            args.Verbs.Add(addRandomMood);
        }
        else
        {
            Verb giveMoods = new()
            {
                Text = "Give Moods",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new ResPath("Interface/Actions/actions_borg.rsi"), "state-laws"),
                Act = () =>
                {
                    if (!EntityManager.EnsureComponent<ThavenMoodsComponent>(args.Target, out moods))
                        _moods.NotifyMoodChange((args.Target, moods));
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("admin-trick-give-moods-description"),
                Priority = (int)TricksVerbPriorities.AddRandomMood,
            };
            args.Verbs.Add(giveMoods);
        }
        #endregion
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
        Electrify = -2,
        Unelectrify = -3,
        EmergencyAccessOn = -4,
        EmergencyAccessOff = -5,
        MakeIndestructible = -6,
        MakeVulnerable = -7,
        BlockUnanchoring = -8,
        RefillBattery = -9,
        DrainBattery = -10,
        RefillOxygen = -11,
        RefillNitrogen = -12,
        RefillPlasma = -13,
        SendToTestArena = -14,
        GrantAllAccess = -15,
        RevokeAllAccess = -16,
        Rejuvenate = -17,
        AdjustStack = -18,
        FillStack = -19,
        Rename = -20,
        Redescribe = -21,
        RenameAndRedescribe = -22,
        BarJobSlots = -23,
        LocateCargoShuttle = -24,
        InfiniteBattery = -25,
        HaltMovement = -26,
        Unpause = -27,
        Pause = -28,
        SnapJoints = -29,
        MakeMinigun = -30,
        SetBulletAmount = -31,
        ToggleOverlays = -32, // #🌟Starlight🌟
        AddRandomMood = -32, //Starlight Thaven
        AddCustomMood = -33, //Starlight Thaven
    }
}
