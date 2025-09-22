using System.Linq;
using System.Numerics;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Stacks;
using Content.Shared.Station.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
    [Dependency] private readonly SharedAirlockSystem _airlockSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedGodmodeSystem _godmodeSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedJointSystem _jointSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;

    protected virtual void AddTricksVerbs(GetVerbsEvent<Verb> args)
    {
        // TODO: Localize these. Will handle soon. ~ Verin
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_sharedAdmin.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (TryComp<DoorBoltComponent>(args.Target, out var bolts))
        {
            Verb bolt = new()
            {
                Text = bolts.BoltsDown ? "Unbolt" : "Bolt",
                Category = VerbCategory.Tricks,
                Icon = bolts.BoltsDown
                    ? new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/unbolt.png"))
                    : new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/bolt.png")),
                Act = () => _doorSystem.SetBoltsDown((args.Target, bolts), !bolts.BoltsDown),
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
                Text = airlockComp.EmergencyAccess ? "Emergency Access Off" : "Emergency Access On",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/emergency_access.png")),
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
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/rejuvenate.png")),
                Act = () => DebugRejuvenateVerb(args.Target),
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
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
                Act = () => _godmodeSystem.EnableGodmode(args.Target),
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
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
                Act = () => _godmodeSystem.DisableGodmode(args.Target),
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-trick-make-vulnerable-description"),
                Priority = (int)TricksVerbPriorities.MakeVulnerable,
            };
            args.Verbs.Add(makeVulnerable);
        }

        if (TryComp<AnchorableComponent>(args.Target, out var anchor))
        {
            Verb blockUnanchor = new()
            {
                Text = "Block Unanchoring",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/anchor.svg.192dpi.png")),
                Act = () => RemComp(args.Target, anchor),
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
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Tanks/oxygen.rsi"), "icon"),
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
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Tanks/red.rsi"), "icon"),
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
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Tanks/plasma.rsi"), "icon"),
                Act = () => RefillGasTank(args.Target, Gas.Plasma, tank),
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
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Tanks/oxygen.rsi"), "icon"),
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
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Tanks/red.rsi"), "icon"),
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
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Tanks/plasma.rsi"), "icon"),
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
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            Act = () => ToolsSendToArenaVerb(player, args.Target),
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
                Act = () => GiveAllAccess(activeId.Value),
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
                Act = () => RevokeAllAccess(activeId.Value),
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
                Act = () => GiveAllAccess(args.Target),
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
                Act = () => RevokeAllAccess(args.Target),
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
                Act = () => ToolsAdjustStackVerb(player, args.Target, stack),
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-adjust-stack-description"),
                Priority = (int) TricksVerbPriorities.AdjustStack,
            };
            args.Verbs.Add(adjustStack);

            Verb fillStack = new()
            {
                Text = "Fill Stack",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/fill-stack.png")),
                Act = () => _stackSystem.SetCount(args.Target, _stackSystem.GetMaxCount(stack), stack),
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-fill-stack-description"),
                Priority = (int) TricksVerbPriorities.FillStack,
            };
            args.Verbs.Add(fillStack);
        }

        Verb rename = new()
        {
            Text = "Rename",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename.png")),
            Act = () => ToolsRenameVerb(player, args.Target),
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-description"),
            Priority = (int) TricksVerbPriorities.Rename,
        };
        args.Verbs.Add(rename);

        Verb redescribe = new()
        {
            Text = "Redescribe",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/redescribe.png")),
            Act = () => ToolsRedescribeVerb(player, args.Target),
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-redescribe-description"),
            Priority = (int) TricksVerbPriorities.Redescribe,
        };
        args.Verbs.Add(redescribe);

        Verb renameAndRedescribe = new()
        {
            Text = "Redescribe",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename_and_redescribe.png")),
            Act = () => ToolsRenameAndRedescribeVerb(player, args.Target),
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-and-redescribe-description"),
            Priority = (int) TricksVerbPriorities.RenameAndRedescribe,
        };
        args.Verbs.Add(renameAndRedescribe);

        if (HasComp<StationDataComponent>(args.Target))
        {
            if (_sharedAdmin.HasAdminFlag(player, AdminFlags.Round))
            {
                Verb barJobSlots = new()
                {
                    Text = "Bar job slots",
                    Category = VerbCategory.Tricks,
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/bar_jobslots.png")),
                    Act = () => ToolsBarJobSlotsVerb(args.Target),
                    Impact = LogImpact.Extreme,
                    Message = Loc.GetString("admin-trick-bar-job-slots-description"),
                    Priority = (int) TricksVerbPriorities.BarJobSlots,
                };
                args.Verbs.Add(barJobSlots);
            }

            Verb locateCargoShuttle = new()
            {
                Text = "Locate Cargo Shuttle",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Soft/cargosoft.rsi"), "icon"),
                Act = () => ToolsLocateCargoShuttleVerb(args.User, args.Target),
                Impact = LogImpact.Low,
                Message = Loc.GetString("admin-trick-locate-cargo-shuttle-description"),
                Priority = (int) TricksVerbPriorities.LocateCargoShuttle,
            };
            args.Verbs.Add(locateCargoShuttle);
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
                    _physicsSystem.SetLinearVelocity(args.Target, Vector2.Zero, body: physics);
                    _physicsSystem.SetAngularVelocity(args.Target, 0f, body: physics);
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-halt-movement-description"),
                Priority = (int) TricksVerbPriorities.HaltMovement,
            };
            args.Verbs.Add(haltMovement);
        }

        if (TryComp<MapComponent>(args.Target, out var map))
        {
            if (_sharedAdmin.HasAdminFlag(player, AdminFlags.Mapping))
            {
                if (_mapSystem.IsPaused(map.MapId))
                {
                    Verb unpauseMap = new()
                    {
                        Text = "Unpause Map",
                        Category = VerbCategory.Tricks,
                        Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/play.png")),
                        Act = () => _mapSystem.SetPaused(map.MapId, false),
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
                        Text = "Pause Map",
                        Category = VerbCategory.Tricks,
                        Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/pause.png")),
                        Act = () => _mapSystem.SetPaused(map.MapId, true),
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
                Text = "Snap Joints",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/snap_joints.png")),
                Act = () => _jointSystem.ClearJoints(args.Target, joints),
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
                Text = "Make Minigun",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Weapons/Guns/HMGs/minigun.rsi"), "icon"),
                Act = () => ToolsMakeMinigunVerb(args.Target, gun),
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
                Text = "Set Bullet Amount",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Fun/caps.rsi"), "mag-6"),
                Act = () => ToolsSetBulletAmountVerb(player, args.Target, ballisticAmmo),
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

    private EntityUid? FindActiveId(EntityUid target)
    {
        if (_inventorySystem.TryGetSlotEntity(target, "id", out var slotEntity))
        {
            if (HasComp<AccessComponent>(slotEntity))
                return slotEntity.Value;

            if (TryComp<PdaComponent>(slotEntity, out var pda) && HasComp<IdCardComponent>(pda.ContainedId))
                return pda.ContainedId;
        }
        else if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var held in _handsSystem.EnumerateHeld((target, hands)))
            {
                if (HasComp<AccessComponent>(held))
                    return held;
            }
        }

        return null;
    }

        private void GiveAllAccess(EntityUid entity)
    {
        var allAccess = _prototypeManager
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => new ProtoId<AccessLevelPrototype>(p.ID))
            .ToArray();

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

    protected virtual void ToolsSendToArenaVerb(ICommonSession player, EntityUid target)
    {
    }

    protected virtual void ToolsAdjustStackVerb(ICommonSession player, EntityUid target, StackComponent stack)
    {
    }

    protected virtual void ToolsRenameVerb(ICommonSession player, EntityUid target)
    {
    }

    protected virtual void ToolsRedescribeVerb(ICommonSession player, EntityUid target)
    {
    }

    protected virtual void ToolsRenameAndRedescribeVerb(ICommonSession player, EntityUid target)
    {
    }

    protected virtual void ToolsBarJobSlotsVerb(EntityUid target)
    {
    }

    protected virtual void ToolsLocateCargoShuttleVerb(EntityUid user, EntityUid target)
    {
    }

    protected virtual void ToolsMakeMinigunVerb(EntityUid target, GunComponent gun)
    {
    }

    protected virtual void ToolsSetBulletAmountVerb(ICommonSession player, EntityUid target, BallisticAmmoProviderComponent ballisticAmmo)
    {
    }
}
