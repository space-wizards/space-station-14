using System.Threading;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GhostKick;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Shared.Administration;
using Content.Shared.Administration.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Clothing.Components;
using Content.Shared.Cluwne;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly CreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly SharedGodmodeSystem _sharedGodmodeSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SuperBonkSystem _superBonkSystem = default!;
    [Dependency] private readonly SlipperySystem _slipperySystem = default!;

    // All smite verbs have names so invokeverb works.
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        Verb explode = new()
        {
            Text = "admin-smite-explode-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Act = () =>
            {
                var coords = _transformSystem.GetMapCoordinates(args.Target);
                Timer.Spawn(_gameTiming.TickPeriod,
                    () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                        4, 1, 2, args.Target, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
                    CancellationToken.None);

                _bodySystem.GibBody(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-explode-description")
        };
        args.Verbs.Add(explode);

        Verb chess = new()
        {
            Text = "admin-smite-chess-dimension-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Tabletop/chessboard.rsi"), "chessboard"),
            Act = () =>
            {
                _sharedGodmodeSystem.EnableGodmode(args.Target); // So they don't suffocate.
                EnsureComp<TabletopDraggableComponent>(args.Target);
                RemComp<PhysicsComponent>(args.Target); // So they can be dragged around.
                var xform = Transform(args.Target);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-chess-self"), args.Target,
                    args.Target, PopupType.LargeCaution);
                _popupSystem.PopupCoordinates(
                    Loc.GetString("admin-smite-chess-others", ("name", args.Target)), xform.Coordinates,
                    Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                var board = Spawn("ChessBoard", xform.Coordinates);
                var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
                xform.Coordinates = EntityCoordinates.FromMap(_mapManager, session.Position);
                xform.WorldRotation = Angle.Zero;
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-chess-dimension-description")
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            Verb flames = new()
            {
                Text = "admin-smite-set-alight-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Alerts/Fire/fire.png")),
                Act = () =>
                {
                    // Fuck you. Burn Forever.
                    flammable.FireStacks = flammable.MaximumFireStacks;
                    _flammableSystem.Ignite(args.Target, args.User);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-set-alight-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-set-alight-others", ("name", args.Target)), xform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-set-alight-description")
            };
            args.Verbs.Add(flames);
        }

        Verb monkey = new()
        {
            Text = "admin-smite-monkeyify-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Animals/monkey.rsi"), "monkey"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminMonkeySmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-monkeyify-description")
        };
        args.Verbs.Add(monkey);

        Verb disposalBin = new()
        {
            Text = "admin-smite-electrocute-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Structures/Piping/disposal.rsi"), "disposal"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminDisposalsSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-garbage-can-description")
        };
        args.Verbs.Add(disposalBin);

        if (TryComp<DamageableComponent>(args.Target, out var damageable) &&
            HasComp<MobStateComponent>(args.Target))
        {
            Verb hardElectrocute = new()
            {
                Text = "admin-smite-creampie-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Hands/Gloves/Color/yellow.rsi"), "icon"),
                Act = () =>
                {
                    int damageToDeal;
                    if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Critical, out var criticalThreshold)) {
                        // We can't crit them so try killing them.
                        if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Dead,
                                out var deadThreshold))
                            return;// whelp.
                        damageToDeal = deadThreshold.Value.Int() - (int) damageable.TotalDamage;
                    }
                    else
                    {
                        damageToDeal = criticalThreshold.Value.Int() - (int) damageable.TotalDamage;
                    }

                    if (damageToDeal <= 0)
                        damageToDeal = 100; // murder time.

                    if (_inventorySystem.TryGetSlots(args.Target, out var slotDefinitions))
                    {
                        foreach (var slot in slotDefinitions)
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var slotEnt))
                                continue;

                            RemComp<InsulatedComponent>(slotEnt.Value); // Fry the gloves.
                        }
                    }

                    _electrocutionSystem.TryDoElectrocution(args.Target, null, damageToDeal,
                        TimeSpan.FromSeconds(30), refresh: true, ignoreInsulation: true);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-electrocute-description")
            };
            args.Verbs.Add(hardElectrocute);
        }

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            Verb creamPie = new()
            {
                Text = "admin-smite-remove-blood-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Consumable/Food/Baked/pie.rsi"), "plain-slice"),
                Act = () =>
                {
                    _creamPieSystem.SetCreamPied(args.Target, creamPied, true);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-creampie-description")
            };
            args.Verbs.Add(creamPie);
        }

        if (TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
        {
            Verb bloodRemoval = new()
            {
                Text = "admin-smite-vomit-organs-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Fluids/tomato_splat.rsi"), "puddle-1"),
                Act = () =>
                {
                    _bloodstreamSystem.SpillAllSolutions(args.Target, bloodstream);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-blood-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-blood-others", ("name", args.Target)), xform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-remove-blood-description")
            };
            args.Verbs.Add(bloodRemoval);
        }

        // bobby...
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            Verb vomitOrgans = new()
            {
                Text = "admin-smite-remove-hands-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Fluids/vomit_toxin.rsi"), "vomit_toxin-1"),
                Act = () =>
                {
                    _vomitSystem.Vomit(args.Target, -1000, -1000); // You feel hollow!
                    var organs = _bodySystem.GetBodyOrganEntityComps<TransformComponent>((args.Target, body));
                    var baseXform = Transform(args.Target);
                    foreach (var organ in organs)
                    {
                        if (HasComp<BrainComponent>(organ.Owner) || HasComp<EyeComponent>(organ.Owner))
                            continue;

                        _transformSystem.PlaceNextTo((organ.Owner, organ.Comp1), (args.Target, baseXform));
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-vomit-organs-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-vomit-organs-others", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-vomit-organs-description")
            };
            args.Verbs.Add(vomitOrgans);

            Verb handsRemoval = new()
            {
                Text = "admin-smite-remove-hand-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/remove-hands.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-remove-hands-description")
            };
            args.Verbs.Add(handsRemoval);

            Verb handRemoval = new()
            {
                Text = "admin-smite-pinball-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/remove-hand.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand, body))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                        break;
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-remove-hand-description")
            };
            args.Verbs.Add(handRemoval);

            Verb stomachRemoval = new()
            {
                Text = "admin-smite-yeet-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Species/Human/organs.rsi"), "stomach"),
                Act = () =>
                {
                    foreach (var entity in _bodySystem.GetBodyOrganEntityComps<StomachComponent>((args.Target, body)))
                    {
                        QueueDel(entity.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-stomach-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-stomach-removal-description"),
            };
            args.Verbs.Add(stomachRemoval);

            Verb lungRemoval = new()
            {
                Text = "admin-smite-become-bread-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Species/Human/organs.rsi"), "lung-r"),
                Act = () =>
                {
                    foreach (var entity in _bodySystem.GetBodyOrganEntityComps<LungComponent>((args.Target, body)))
                    {
                        QueueDel(entity.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-lung-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-lung-removal-description"),
            };
            args.Verbs.Add(lungRemoval);
        }

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            Verb pinball = new()
            {
                Text = "admin-smite-ghostkick-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/toys.rsi"), "basketball"),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    xform.Anchored = false; // Just in case.
                    _physics.SetBodyType(args.Target, BodyType.Dynamic, manager: fixtures, body: physics);
                    _physics.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        _physics.SetRestitution(args.Target, fixture, 1.1f, false, fixtures);
                    }

                    _fixtures.FixtureUpdate(args.Target, manager: fixtures, body: physics);

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(args.Target, physics, 0f);
                    _physics.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-pinball-description")
            };
            args.Verbs.Add(pinball);

            Verb yeet = new()
            {
                Text = "admin-smite-nyanify-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    xform.Anchored = false; // Just in case.

                    _physics.SetBodyType(args.Target, BodyType.Dynamic, body: physics);
                    _physics.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        _physics.SetHard(args.Target, fixture, false, manager: fixtures);
                    }

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(8.0f, 8.0f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(args.Target, physics, 0f);
                    _physics.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-yeet-description")
            };
            args.Verbs.Add(yeet);
        }

        Verb bread = new()
        {
            Text = "admin-smite-kill-sign-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Consumable/Food/Baked/bread.rsi"), "plain"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminBreadSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-become-bread-description")
        };
        args.Verbs.Add(bread);

        Verb mouse = new()
        {
            Text = "admin-smite-cluwne-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Animals/mouse.rsi"), "icon-0"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminMouseSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-become-mouse-description")
        };
        args.Verbs.Add(mouse);

        if (TryComp<ActorComponent>(args.Target, out var actorComponent))
        {
            Verb ghostKick = new()
            {
                Text = "admin-smite-anger-pointing-arrows-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/gavel.svg.192dpi.png")),
                Act = () =>
                {
                    _ghostKickManager.DoDisconnect(actorComponent.PlayerSession.Channel, "Smitten.");
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-ghostkick-description")
            };
            args.Verbs.Add(ghostKick);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventory)) {
            Verb nyanify = new()
            {
                Text = "admin-smite-dust-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Head/Hats/catears.rsi"), "icon"),
                Act = () =>
                {
                    var ears = Spawn("ClothingHeadHatCatEars", Transform(args.Target).Coordinates);
                    EnsureComp<UnremoveableComponent>(ears);
                    _inventorySystem.TryUnequip(args.Target, "head", true, true, false, inventory);
                    _inventorySystem.TryEquip(args.Target, ears, "head", true, true, false, inventory);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-nyanify-description")
            };
            args.Verbs.Add(nyanify);

            Verb killSign = new()
            {
                Text = "admin-smite-buffering-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Misc/killsign.rsi"), "icon"),
                Act = () =>
                {
                    EnsureComp<KillSignComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-kill-sign-description")
            };
            args.Verbs.Add(killSign);

            Verb cluwne = new()
            {
                Text = "admin-smite-become-instrument-name",
                Category = VerbCategory.Smite,

                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Mask/cluwne.rsi"), "icon"),

                Act = () =>
                {
                    EnsureComp<CluwneComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-cluwne-description")
            };
            args.Verbs.Add(cluwne);

            Verb maiden = new()
            {
                Text = "admin-smite-remove-gravity-name",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Uniforms/Jumpskirt/janimaid.rsi"), "icon"),
                Act = () =>
                {
                    SetOutfitCommand.SetOutfit(args.Target, "JanitorMaidGear", EntityManager, (_, clothing) =>
                    {
                        if (HasComp<ClothingComponent>(clothing))
                            EnsureComp<UnremoveableComponent>(clothing);
                        EnsureComp<ClumsyComponent>(args.Target);
                    });
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-maid-description")
            };
            args.Verbs.Add(maiden);
        }

        Verb angerPointingArrows = new()
        {
            Text = "admin-smite-reptilian-species-swap-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Interface/Misc/pointing.rsi"), "pointing"),
            Act = () =>
            {
                EnsureComp<PointingArrowAngeringComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-anger-pointing-arrows-description")
        };
        args.Verbs.Add(angerPointingArrows);

        Verb dust = new()
        {
            Text = "admin-smite-locker-stuff-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Materials/materials.rsi"), "ash"),
            Act = () =>
            {
                EntityManager.QueueDeleteEntity(args.Target);
                Spawn("Ash", Transform(args.Target).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-dust-description"),
        };
        args.Verbs.Add(dust);

        Verb youtubeVideoSimulation = new()
        {
            Text = "admin-smite-headstand-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Misc/buffering_smite_icon.png")),
            Act = () =>
            {
                EnsureComp<BufferingComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-buffering-description"),
        };
        args.Verbs.Add(youtubeVideoSimulation);

        Verb instrumentation = new()
        {
            Text = "admin-smite-become-mouse-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Instruments/h_synthesizer.rsi"), "icon"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminInstrumentSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-become-instrument-description"),
        };
        args.Verbs.Add(instrumentation);

        Verb noGravity = new()
        {
            Text = "admin-smite-maid-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Structures/Machines/gravity_generator.rsi"), "off"),
            Act = () =>
            {
                var grav = EnsureComp<MovementIgnoreGravityComponent>(args.Target);
                grav.Weightless = true;

                Dirty(args.Target, grav);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-remove-gravity-description"),
        };
        args.Verbs.Add(noGravity);

        Verb reptilian = new()
        {
            Text = "admin-smite-zoom-in-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/toys.rsi"), "plushie_lizard"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminLizardSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-reptilian-species-swap-description"),
        };
        args.Verbs.Add(reptilian);

        Verb locker = new()
        {
            Text = "admin-smite-flip-eye-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Structures/Storage/closet.rsi"), "generic"),
            Act = () =>
            {
                var xform = Transform(args.Target);
                var locker = Spawn("ClosetMaintenance", xform.Coordinates);
                if (TryComp<EntityStorageComponent>(locker, out var storage))
                {
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                    _entityStorageSystem.Insert(args.Target, locker, storage);
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                }
                _weldableSystem.SetWeldedState(locker, true);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-locker-stuff-description"),
        };
        args.Verbs.Add(locker);

        Verb headstand = new()
        {
            Text = "admin-smite-run-walk-swap-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Act = () =>
            {
                EnsureComp<HeadstandComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-headstand-description"),
        };
        args.Verbs.Add(headstand);

        Verb zoomIn = new()
        {
            Text = "admin-smite-super-speed-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/zoom.png")),
            Act = () =>
            {
                var eye = EnsureComp<ContentEyeComponent>(args.Target);
                _eyeSystem.SetZoom(args.Target, eye.TargetZoom * 0.2f, ignoreLimits: true);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-zoom-in-description"),
        };
        args.Verbs.Add(zoomIn);

        Verb flipEye = new()
        {
            Text = "admin-smite-stomach-removal-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/flip.png")),
            Act = () =>
            {
                var eye = EnsureComp<ContentEyeComponent>(args.Target);
                _eyeSystem.SetZoom(args.Target, eye.TargetZoom * -1, ignoreLimits: true);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-flip-eye-description"),
        };
        args.Verbs.Add(flipEye);

        Verb runWalkSwap = new()
        {
            Text = "admin-smite-speak-backwards-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/run-walk-swap.png")),
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) = (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

                Dirty(args.Target, movementSpeed);

                _popupSystem.PopupEntity(Loc.GetString("admin-smite-run-walk-swap-prompt"), args.Target,
                    args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-run-walk-swap-description"),
        };
        args.Verbs.Add(runWalkSwap);

        Verb backwardsAccent = new()
        {
            Text = "admin-smite-lung-removal-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/help-backwards.png")),
            Act = () =>
            {
                EnsureComp<BackwardsAccentComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-speak-backwards-description"),
        };
        args.Verbs.Add(backwardsAccent);

        Verb disarmProne = new()
        {
            Text = "admin-smite-disarm-prone-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Actions/disarm.png")),
            Act = () =>
            {
                EnsureComp<DisarmProneComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-disarm-prone-description"),
        };
        args.Verbs.Add(disarmProne);

        Verb superSpeed = new()
        {
            Text = "admin-smite-garbage-can-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/super_speed.png")),
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                _movementSpeedModifierSystem?.ChangeBaseSpeed(args.Target, 400, 8000, 40, movementSpeed);

                _popupSystem.PopupEntity(Loc.GetString("admin-smite-super-speed-prompt"), args.Target,
                    args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-super-speed-description"),
        };
        args.Verbs.Add(superSpeed);

        //Bonk
        Verb superBonkLite = new()
        {
            Text = "admin-smite-super-bonk-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Structures/Furniture/Tables/glass.rsi"), "full"),
            Act = () =>
            {
                _superBonkSystem.StartSuperBonk(args.Target, stopWhenDead: true);
            },
            Message = Loc.GetString("admin-smite-super-bonk-lite-description"),
            Impact = LogImpact.Extreme,
        };
        args.Verbs.Add(superBonkLite);
        Verb superBonk= new()
        {
            Text = "admin-smite-super-bonk-lite-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Structures/Furniture/Tables/generic.rsi"), "full"),
            Act = () =>
            {
                _superBonkSystem.StartSuperBonk(args.Target);
            },
            Message = Loc.GetString("admin-smite-super-bonk-description"),
            Impact = LogImpact.Extreme,
        };
        args.Verbs.Add(superBonk);

        Verb superslip = new()
        {
            Text = "admin-smite-super-slip-name",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Objects/Specific/Janitorial/soap.rsi"), "omega-4"),
            Act = () =>
            {
                var hadSlipComponent = EnsureComp(args.Target, out SlipperyComponent slipComponent);
                if (!hadSlipComponent)
                {
                    slipComponent.SuperSlippery = true;
                    slipComponent.ParalyzeTime = 5;
                    slipComponent.LaunchForwardsMultiplier = 20;
                }

                _slipperySystem.TrySlip(args.Target, slipComponent, args.Target, requiresContact: false);
                if (!hadSlipComponent)
                {
                    RemComp(args.Target, slipComponent);
                }
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-super-slip-description")
        };
        args.Verbs.Add(superslip);
    }
}
