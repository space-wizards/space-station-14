using System.Linq;
using System.Threading;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Clothing.Components;
using Content.Server.Damage.Systems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GhostKick;
using Content.Server.Interaction.Components;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Disease;
using Content.Shared.Electrocution;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tabletop.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly PolymorphableSystem _polymorphableSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly CreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GodmodeSystem _godmodeSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    // All smite verbs have names so invokeverb works.
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        Verb explode = new()
        {
            Text = "Explode",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
            Act = () =>
            {
                var coords = Transform(args.Target).MapPosition;
                Timer.Spawn(_gameTiming.TickPeriod,
                    () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                        4, 1, 2, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
                    CancellationToken.None);

                if (TryComp(args.Target, out SharedBodyComponent? body))
                {
                    body.Gib();
                }
            },
            Impact = LogImpact.Extreme,
            Message = "Explode them.",
        };
        args.Verbs.Add(explode);

        Verb chess = new()
        {
            Text = "Chess Dimension",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Objects/Fun/Tabletop/chessboard.rsi/chessboard.png",
            Act = () =>
            {
                _godmodeSystem.EnableGodmode(args.Target); // So they don't suffocate.
                EnsureComp<TabletopDraggableComponent>(args.Target);
                RemComp<PhysicsComponent>(args.Target); // So they can be dragged around.
                var xform = Transform(args.Target);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-chess-self"), args.Target, Filter.Entities(args.Target));
                _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-chess-others", ("name", args.Target)), xform.Coordinates, Filter.Pvs(args.Target).RemoveWhereAttachedEntity(x => x == args.Target));
                var board = Spawn("ChessBoard", xform.Coordinates);
                var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
                xform.Coordinates = EntityCoordinates.FromMap(_mapManager, session.Position);
                xform.WorldRotation = Angle.Zero;
            },
            Impact = LogImpact.Extreme,
            Message = "Banishment to the Chess Dimension.",
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            Verb flames = new()
            {
                Text = "Set Alight",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Interface/Alerts/Fire/fire.png",
                Act = () =>
                {
                    // Fuck you. Burn Forever.
                    flammable.FireStacks = 99999.9f;
                    _flammableSystem.Ignite(args.Target);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-set-alight-self"), args.Target, Filter.Entities(args.Target));
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-set-alight-others", ("name", args.Target)), xform.Coordinates, Filter.Pvs(args.Target).RemoveWhereAttachedEntity(x => x == args.Target));
                },
                Impact = LogImpact.Extreme,
                Message = "Makes them burn.",
            };
            args.Verbs.Add(flames);
        }

        Verb monkey = new()
        {
            Text = "Monkeyify",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Mobs/Animals/monkey.rsi/dead.png",
            Act = () =>
            {
                _polymorphableSystem.PolymorphEntity(args.Target, "AdminMonkeySmite");
            },
            Impact = LogImpact.Extreme,
            Message = "Monkey mode.",
        };
        args.Verbs.Add(monkey);

        if (TryComp<DiseaseCarrierComponent>(args.Target, out var carrier))
        {
            Verb lungCancer = new()
            {
                Text = "Lung Cancer",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Mobs/Species/Human/organs.rsi/lung-l.png",
                Act = () =>
                {
                    _diseaseSystem.TryInfect(carrier, _prototypeManager.Index<DiseasePrototype>("StageIIIALungCancer"),
                        1.0f, true);
                },
                Impact = LogImpact.Extreme,
                Message = "Stage IIIA Lung Cancer, for when they really like the hit show Breaking Bad.",
            };
            args.Verbs.Add(lungCancer);
        }

        if (TryComp<DamageableComponent>(args.Target, out var damageable) &&
            TryComp<MobStateComponent>(args.Target, out var mobState))
        {
            Verb hardElectrocute = new()
            {
                Text = "Electrocute",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Clothing/Hands/Gloves/Color/yellow.rsi/icon.png",
                Act = () =>
                {
                    int damageToDeal;
                    var critState = mobState._highestToLowestStates.Where(x => x.Value.IsCritical()).FirstOrNull();
                    if (critState is null)
                    {
                        // We can't crit them so try killing them.
                        var deadState = mobState._highestToLowestStates.Where(x => x.Value.IsDead()).FirstOrNull();
                        if (deadState is null)
                            return; // whelp.

                        damageToDeal = deadState.Value.Key - (int) damageable.TotalDamage;
                    }
                    else
                    {
                        damageToDeal = critState.Value.Key - (int) damageable.TotalDamage;
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
                        TimeSpan.FromSeconds(30), true);
                },
                Impact = LogImpact.Extreme,
                Message = "Electrocutes them, rendering anything they were wearing useless.",
            };
            args.Verbs.Add(hardElectrocute);
        }

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            Verb creamPie = new()
            {
                Text = "Creampie",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Objects/Consumable/Food/Baked/pie.rsi/plain-slice.png",
                Act = () =>
                {
                    _creamPieSystem.SetCreamPied(args.Target, creamPied, true);
                },
                Impact = LogImpact.Extreme,
                Message = "A cream pie, condensed into a button.",
            };
            args.Verbs.Add(creamPie);
        }

        if (TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
        {
            Verb bloodRemoval = new()
            {
                Text = "Remove blood",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Fluids/tomato_splat.rsi/puddle-1.png",
                Act = () =>
                {
                    _bloodstreamSystem.SpillAllSolutions(args.Target, bloodstream);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-blood-self"), args.Target, Filter.Entities(args.Target));
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-blood-others", ("name", args.Target)), xform.Coordinates, Filter.Pvs(args.Target).RemoveWhereAttachedEntity(x => x == args.Target));
                },
                Impact = LogImpact.Extreme,
                Message = "Removes their blood. All of it.",
            };
            args.Verbs.Add(bloodRemoval);
        }

        // bobby...
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            Verb vomitOrgans = new()
            {
                Text = "Vomit organs",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Fluids/vomit_toxin.rsi/vomit_toxin-1.png",
                Act = () =>
                {
                    _vomitSystem.Vomit(args.Target, -1000, -1000); // You feel hollow!
                    var organs = _bodySystem.GetComponentsOnMechanisms<TransformComponent>(args.Target, body);
                    var baseXform = Transform(args.Target);
                    foreach (var (xform, mechanism) in organs)
                    {
                        if (HasComp<BrainComponent>(xform.Owner) || HasComp<EyeComponent>(xform.Owner))
                            continue;

                        mechanism.Part?.RemoveMechanism(mechanism);
                        xform.Coordinates = baseXform.Coordinates.Offset(_random.NextVector2(0.5f,0.75f));
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-vomit-organs-self"), args.Target, Filter.Entities(args.Target));
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-vomit-organs-others", ("name", args.Target)), baseXform.Coordinates, Filter.Pvs(args.Target).RemoveWhereAttachedEntity(x => x == args.Target));
                },
                Impact = LogImpact.Extreme,
                Message = "Causes them to vomit, including their internal organs.",
            };
            args.Verbs.Add(vomitOrgans);

            Verb handRemoval = new()
            {
                Text = "Remove hands",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Interface/fist.svg.192dpi.png",
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in body.GetPartsOfType(BodyPartType.Hand))
                    {
                        body.RemovePart(part);
                        Transform(part.Owner).Coordinates = baseXform.Coordinates;
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target, Filter.Entities(args.Target));
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-others", ("name", args.Target)), baseXform.Coordinates, Filter.Pvs(args.Target).RemoveWhereAttachedEntity(x => x == args.Target));
                },
                Impact = LogImpact.Extreme,
                Message = "Removes the target's hands.",
            };
            args.Verbs.Add(handRemoval);
        }

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            Verb pinball = new()
            {
                Text = "Pinball",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Objects/Fun/toys.rsi/basketball.png",
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    xform.Anchored = false; // Just in case.
                    physics.BodyType = BodyType.Dynamic;
                    physics.BodyStatus = BodyStatus.InAir;
                    physics.WakeBody();
                    foreach (var (_, fixture) in fixtures.Fixtures)
                    {
                        if (!fixture.Hard)
                            continue;
                        fixture.Restitution = 1.1f;
                    }

                    physics.LinearVelocity = _random.NextVector2(1.5f, 1.5f);
                    physics.AngularVelocity = MathF.PI * 12;
                    physics.LinearDamping = 0.0f;
                    physics.AngularDamping = 0.0f;
                },
                Impact = LogImpact.Extreme,
                Message =
                    "Turns them into a super bouncy ball, flinging them around until they clip through the station into the abyss.",
            };
            args.Verbs.Add(pinball);

            Verb yeet = new()
            {
                Text = "Yeet",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png",
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    xform.Anchored = false; // Just in case.
                    physics.BodyType = BodyType.Dynamic;
                    physics.BodyStatus = BodyStatus.InAir;
                    physics.WakeBody();
                    foreach (var (_, fixture) in fixtures.Fixtures)
                    {
                        fixture.Hard = false;
                    }

                    physics.LinearVelocity = _random.NextVector2(8.0f, 8.0f);
                    physics.AngularVelocity = MathF.PI * 12;
                    physics.LinearDamping = 0.0f;
                    physics.AngularDamping = 0.0f;
                },
                Impact = LogImpact.Extreme,
                Message = "Banishes them into the depths of space by turning on no-clip and tossing them.",
            };
            args.Verbs.Add(yeet);
        }

        Verb bread = new()
        {
            Text = "Become Bread",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Objects/Consumable/Food/Baked/bread.rsi/plain.png",
            Act = () =>
            {
                _polymorphableSystem.PolymorphEntity(args.Target, "AdminBreadSmite");
            },
            Impact = LogImpact.Extreme,
            Message = "It turns them into bread. Really. That's all it does.",
        };
        args.Verbs.Add(bread);

        if (TryComp<ActorComponent>(args.Target, out var actorComponent))
        {
            Verb ghostKick = new()
            {
                Text = "Ghostkick",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Interface/gavel.svg.192dpi.png",
                Act = () =>
                {
                    _ghostKickManager.DoDisconnect(actorComponent.PlayerSession.ConnectedClient, "Smitten.");
                },
                Impact = LogImpact.Extreme,
                Message = "Silently kicks the user, dropping their connection.",
            };
            args.Verbs.Add(ghostKick);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventory)) {
            Verb nyanify = new()
            {
                Text = "Nyanify",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Clothing/Head/Hats/catears.rsi/icon.png",
                Act = () =>
                {
                    var ears = Spawn("ClothingHeadHatCatEars", Transform(args.Target).Coordinates);
                    EnsureComp<UnremoveableComponent>(ears);
                    _inventorySystem.TryUnequip(args.Target, "head", true, true, false, inventory);
                    _inventorySystem.TryEquip(args.Target, ears, "head", true, true, false, inventory);
                },
                Impact = LogImpact.Extreme,
                Message = "Forcibly adds cat ears. There is no escape.",
            };
            args.Verbs.Add(nyanify);

            Verb killSign = new()
            {
                Text = "Kill sign",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Objects/Misc/killsign.rsi/icon.png",
                Act = () =>
                {
                    EnsureComp<KillSignComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = "Marks a player for death by their fellows.",
            };
            args.Verbs.Add(killSign);

            // TODO: Port cluwne outfit.
            Verb clown = new()
            {
                Text = "Clown",
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Objects/Fun/bikehorn.rsi/icon.png",
                Act = () =>
                {
                    SetOutfitCommand.SetOutfit(args.Target, "ClownGear", EntityManager, (_, clothing) =>
                    {
                        if (HasComp<ClothingComponent>(clothing))
                            EnsureComp<UnremoveableComponent>(clothing);
                        EnsureComp<ClumsyComponent>(args.Target);
                    });
                },
                Impact = LogImpact.Extreme,
                Message = "Clowns them. The suit cannot be removed.",
            };
            args.Verbs.Add(clown);
        }

        Verb angerPointingArrows = new()
        {
            Text = "Anger Pointing Arrows",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Interface/Misc/pointing.rsi/pointing.png",
            Act = () =>
            {
                EnsureComp<PointingArrowAngeringComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = "Angers the pointing arrows, causing them to assault this entity.",
        };
        args.Verbs.Add(angerPointingArrows);

        Verb dust = new()
        {
            Text = "Dust",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Objects/Materials/materials.rsi/ash.png",
            Act = () =>
            {
                EntityManager.QueueDeleteEntity(args.Target);
                Spawn("Ash", Transform(args.Target).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target, Filter.Pvs(args.Target));
            },
            Impact = LogImpact.Extreme,
            Message = "Reduces the target to a small pile of ash.",
        };
        args.Verbs.Add(dust);

        Verb youtubeVideoSimulation = new()
        {
            Text = "Buffering",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Interface/Misc/buffering_smite_icon.png",
            Act = () =>
            {
                EnsureComp<BufferingComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = "Causes the target to randomly start buffering, freezing them in place for a short timespan while they load.",
        };
        args.Verbs.Add(youtubeVideoSimulation);

        Verb instrumentation = new()
        {
            Text = "Become Instrument",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Objects/Fun/Instruments/h_synthesizer.rsi/icon.png",
            Act = () =>
            {
                _polymorphableSystem.PolymorphEntity(args.Target, "AdminInstrumentSmite");
            },
            Impact = LogImpact.Extreme,
            Message = "It turns them into a supersynth. Really. That's all it does.",
        };
        args.Verbs.Add(instrumentation);

        Verb noGravity = new()
        {
            Text = "Remove gravity",
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Structures/Machines/gravity_generator.rsi/off.png",
            Act = () =>
            {
                var grav = EnsureComp<MovementIgnoreGravityComponent>(args.Target);
                grav.Weightless = true;
            },
            Impact = LogImpact.Extreme,
            Message = "Grants them anti-gravity.",
        };
        args.Verbs.Add(noGravity);
    }
}
