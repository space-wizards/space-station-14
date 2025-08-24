using System.Threading;
using Content.Server.Administration.Components;
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
using Content.Server.Storage.EntitySystems;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Shared.Administration;
using Content.Shared.Administration.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Clumsy;
using Content.Shared.Clothing.Components;
using Content.Shared.Cluwne;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Storage.Components;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly SharedGodmodeSystem _sharedGodmodeSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SuperBonkSystem _superBonkSystem = default!;
    [Dependency] private readonly SlipperySystem _slipperySystem = default!;

    protected override void PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
    {
        // When/If polymorphsystem gets shared, this should be removed.
        _polymorphSystem.PolymorphEntity(uid, protoId);
    }

    protected override void SmiteExplodeVerb(EntityUid target)
    {
        var coords = _transformSystem.GetMapCoordinates(target);
        Timer.Spawn(_gameTiming.TickPeriod,
            () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                4,
                1,
                2,
                target,
                maxTileBreak: 0), // it gibs, damage doesn't need to be high.
            CancellationToken.None);

        _bodySystem.GibBody(target);
    }

    protected override void SmiteChessVerb(EntityUid target)
    {
        _sharedGodmodeSystem.EnableGodmode(target); // So they don't suffocate.
        EnsureComp<TabletopDraggableComponent>(target);
        RemComp<PhysicsComponent>(target); // So they can be dragged around.
        var xform = Transform(target);
        _popupSystem.PopupEntity(Loc.GetString("admin-smite-chess-self"),
            target,
            target,
            PopupType.LargeCaution);
        _popupSystem.PopupCoordinates(
            Loc.GetString("admin-smite-chess-others", ("name", target)),
            xform.Coordinates,
            Filter.PvsExcept(target),
            true,
            PopupType.MediumCaution);
        var board = Spawn("ChessBoard", xform.Coordinates);
        var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
        _transformSystem.SetMapCoordinates(target, session.Position);
        _transformSystem.SetWorldRotationNoLerp((target, xform), Angle.Zero);
    }

    protected override void SmiteSetAlightVerb(EntityUid user, EntityUid target, FlammableComponent flammable)
    {
        // Fuck you. Burn Forever.
        flammable.FireStacks = flammable.MaximumFireStacks;
        _flammableSystem.Ignite(target, user);
        var xform = Transform(target);
        _popupSystem.PopupEntity(Loc.GetString("admin-smite-set-alight-self"),
            target,
            target,
            PopupType.LargeCaution);
        _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-set-alight-others", ("name", target)),
            xform.Coordinates,
            Filter.PvsExcept(target),
            true,
            PopupType.MediumCaution);
    }

    protected override void SmiteVomitOrgansVerb(EntityUid target, BodyComponent body)
    {
        _vomitSystem.Vomit(target, -1000, -1000); // You feel hollow!
        var organs = _bodySystem.GetBodyOrganEntityComps<TransformComponent>((target, body));
        var baseXform = Transform(target);
        foreach (var organ in organs)
        {
            if (HasComp<BrainComponent>(organ.Owner) || HasComp<EyeComponent>(organ.Owner))
                continue;

            _transformSystem.PlaceNextTo((organ.Owner, organ.Comp1), (target, baseXform));
        }

        _popupSystem.PopupEntity(Loc.GetString("admin-smite-vomit-organs-self"),
            target,
            target,
            PopupType.LargeCaution);
        _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-vomit-organs-others", ("name", target)),
            baseXform.Coordinates,
            Filter.PvsExcept(target),
            true,
            PopupType.MediumCaution);
    }

    protected override void SmiteRemoveLungsVerb(EntityUid target, BodyComponent body)
    {
        foreach (var entity in _bodySystem.GetBodyOrganEntityComps<LungComponent>((target, body)))
        {
            QueueDel(entity.Owner);
        }

        _popupSystem.PopupEntity(Loc.GetString("admin-smite-lung-removal-self"),
            target,
            target,
            PopupType.LargeCaution);
    }

    // All smite verbs have names so invokeverb works.
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            var pinballName = Loc.GetString("admin-smite-pinball-name").ToLowerInvariant();
            Verb pinball = new()
            {
                Text = pinballName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Balls/basketball.rsi"), "icon"),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target, xform); // Just in case.
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
                Message = string.Join(": ", pinballName, Loc.GetString("admin-smite-pinball-description"))
            };
            args.Verbs.Add(pinball);

            var yeetName = Loc.GetString("admin-smite-yeet-name").ToLowerInvariant();
            Verb yeet = new()
            {
                Text = yeetName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target); // Just in case.

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
                Message = string.Join(": ", yeetName, Loc.GetString("admin-smite-yeet-description"))
            };
            args.Verbs.Add(yeet);
        }

        var breadName = Loc.GetString("admin-smite-become-bread-name").ToLowerInvariant(); // Will I get cancelled for breadName-ing you?
        Verb bread = new()
        {
            Text = breadName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Consumable/Food/Baked/bread.rsi"), "plain"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminBreadSmite");
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", breadName, Loc.GetString("admin-smite-become-bread-description"))
        };
        args.Verbs.Add(bread);

        var mouseName = Loc.GetString("admin-smite-become-mouse-name").ToLowerInvariant();
        Verb mouse = new()
        {
            Text = mouseName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Animals/mouse.rsi"), "icon-0"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminMouseSmite");
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", mouseName, Loc.GetString("admin-smite-become-mouse-description"))
        };
        args.Verbs.Add(mouse);

        if (TryComp<ActorComponent>(args.Target, out var actorComponent))
        {
            var ghostKickName = Loc.GetString("admin-smite-ghostkick-name").ToLowerInvariant();
            Verb ghostKick = new()
            {
                Text = ghostKickName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/gavel.svg.192dpi.png")),
                Act = () =>
                {
                    _ghostKickManager.DoDisconnect(actorComponent.PlayerSession.Channel, "Smitten.");
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", ghostKickName, Loc.GetString("admin-smite-ghostkick-description"))

            };
            args.Verbs.Add(ghostKick);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventory))
        {
            var nyanifyName = Loc.GetString("admin-smite-nyanify-name").ToLowerInvariant();
            Verb nyanify = new()
            {
                Text = nyanifyName,
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
                Message = string.Join(": ", nyanifyName, Loc.GetString("admin-smite-nyanify-description"))
            };
            args.Verbs.Add(nyanify);

            var killSignName = Loc.GetString("admin-smite-kill-sign-name").ToLowerInvariant();
            Verb killSign = new()
            {
                Text = killSignName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Misc/killsign.rsi"), "icon"),
                Act = () =>
                {
                    EnsureComp<KillSignComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", killSignName, Loc.GetString("admin-smite-kill-sign-description"))
            };
            args.Verbs.Add(killSign);

            var cluwneName = Loc.GetString("admin-smite-cluwne-name").ToLowerInvariant();
            Verb cluwne = new()
            {
                Text = cluwneName,
                Category = VerbCategory.Smite,

                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Mask/cluwne.rsi"), "icon"),

                Act = () =>
                {
                    EnsureComp<CluwneComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", cluwneName, Loc.GetString("admin-smite-cluwne-description"))
            };
            args.Verbs.Add(cluwne);

            var maidenName = Loc.GetString("admin-smite-maid-name").ToLowerInvariant();
            Verb maiden = new()
            {
                Text = maidenName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Uniforms/Jumpskirt/janimaid.rsi"), "icon"),
                Act = () =>
                {
                    _outfit.SetOutfit(args.Target, "JanitorMaidGear", (_, clothing) =>
                    {
                        if (HasComp<ClothingComponent>(clothing))
                            EnsureComp<UnremoveableComponent>(clothing);
                        EnsureComp<ClumsyComponent>(args.Target);
                    });
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", maidenName, Loc.GetString("admin-smite-maid-description"))
            };
            args.Verbs.Add(maiden);
        }

        var angerPointingArrowsName = Loc.GetString("admin-smite-anger-pointing-arrows-name").ToLowerInvariant();
        Verb angerPointingArrows = new()
        {
            Text = angerPointingArrowsName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Interface/Misc/pointing.rsi"), "pointing"),
            Act = () =>
            {
                EnsureComp<PointingArrowAngeringComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", angerPointingArrowsName, Loc.GetString("admin-smite-anger-pointing-arrows-description"))
        };
        args.Verbs.Add(angerPointingArrows);

        var dustName = Loc.GetString("admin-smite-dust-name").ToLowerInvariant();
        Verb dust = new()
        {
            Text = dustName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Materials/materials.rsi"), "ash"),
            Act = () =>
            {
                QueueDel(args.Target);
                Spawn("Ash", Transform(args.Target).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", dustName, Loc.GetString("admin-smite-dust-description"))
        };
        args.Verbs.Add(dust);

        var youtubeVideoSimulationName = Loc.GetString("admin-smite-buffering-name").ToLowerInvariant();
        Verb youtubeVideoSimulation = new()
        {
            Text = youtubeVideoSimulationName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Misc/buffering_smite_icon.png")),
            Act = () =>
            {
                EnsureComp<BufferingComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", youtubeVideoSimulationName, Loc.GetString("admin-smite-buffering-description"))
        };
        args.Verbs.Add(youtubeVideoSimulation);

        var instrumentationName = Loc.GetString("admin-smite-become-instrument-name").ToLowerInvariant();
        Verb instrumentation = new()
        {
            Text = instrumentationName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Instruments/h_synthesizer.rsi"), "supersynth"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminInstrumentSmite");
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", instrumentationName, Loc.GetString("admin-smite-become-instrument-description"))
        };
        args.Verbs.Add(instrumentation);

        var noGravityName = Loc.GetString("admin-smite-remove-gravity-name").ToLowerInvariant();
        Verb noGravity = new()
        {
            Text = noGravityName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Structures/Machines/gravity_generator.rsi"), "off"),
            Act = () =>
            {
                var grav = EnsureComp<MovementIgnoreGravityComponent>(args.Target);
                grav.Weightless = true;

                Dirty(args.Target, grav);

                EnsureComp<GravityAffectedComponent>(args.Target, out var weightless);
                weightless.Weightless = true;

                Dirty(args.Target, weightless);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", noGravityName, Loc.GetString("admin-smite-remove-gravity-description"))
        };
        args.Verbs.Add(noGravity);

        var reptilianName = Loc.GetString("admin-smite-reptilian-species-swap-name").ToLowerInvariant();
        Verb reptilian = new()
        {
            Text = reptilianName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Plushies/lizard.rsi"), "icon"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminLizardSmite");
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", reptilianName, Loc.GetString("admin-smite-reptilian-species-swap-description"))
        };
        args.Verbs.Add(reptilian);

        var lockerName = Loc.GetString("admin-smite-locker-stuff-name").ToLowerInvariant();
        Verb locker = new()
        {
            Text = lockerName,
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
            Message = string.Join(": ", lockerName, Loc.GetString("admin-smite-locker-stuff-description"))
        };
        args.Verbs.Add(locker);

        var headstandName = Loc.GetString("admin-smite-headstand-name").ToLowerInvariant();
        Verb headstand = new()
        {
            Text = headstandName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Act = () =>
            {
                EnsureComp<HeadstandComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", headstandName, Loc.GetString("admin-smite-headstand-description"))
        };
        args.Verbs.Add(headstand);

        var zoomInName = Loc.GetString("admin-smite-zoom-in-name").ToLowerInvariant();
        Verb zoomIn = new()
        {
            Text = zoomInName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/zoom.png")),
            Act = () =>
            {
                var eye = EnsureComp<ContentEyeComponent>(args.Target);
                _eyeSystem.SetZoom(args.Target, eye.TargetZoom * 0.2f, ignoreLimits: true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", zoomInName, Loc.GetString("admin-smite-zoom-in-description"))
        };
        args.Verbs.Add(zoomIn);

        var flipEyeName = Loc.GetString("admin-smite-flip-eye-name").ToLowerInvariant();
        Verb flipEye = new()
        {
            Text = flipEyeName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/flip.png")),
            Act = () =>
            {
                var eye = EnsureComp<ContentEyeComponent>(args.Target);
                _eyeSystem.SetZoom(args.Target, eye.TargetZoom * -1, ignoreLimits: true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", flipEyeName, Loc.GetString("admin-smite-flip-eye-description"))
        };
        args.Verbs.Add(flipEye);

        var runWalkSwapName = Loc.GetString("admin-smite-run-walk-swap-name").ToLowerInvariant();
        Verb runWalkSwap = new()
        {
            Text = runWalkSwapName,
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
            Message = string.Join(": ", runWalkSwapName, Loc.GetString("admin-smite-run-walk-swap-description"))
        };
        args.Verbs.Add(runWalkSwap);

        var backwardsAccentName = Loc.GetString("admin-smite-speak-backwards-name").ToLowerInvariant();
        Verb backwardsAccent = new()
        {
            Text = backwardsAccentName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/help-backwards.png")),
            Act = () =>
            {
                EnsureComp<BackwardsAccentComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", backwardsAccentName, Loc.GetString("admin-smite-speak-backwards-description"))
        };
        args.Verbs.Add(backwardsAccent);

        var disarmProneName = Loc.GetString("admin-smite-disarm-prone-name").ToLowerInvariant();
        Verb disarmProne = new()
        {
            Text = disarmProneName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Actions/disarm.png")),
            Act = () =>
            {
                EnsureComp<DisarmProneComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", disarmProneName, Loc.GetString("admin-smite-disarm-prone-description"))
        };
        args.Verbs.Add(disarmProne);

        var superSpeedName = Loc.GetString("admin-smite-super-speed-name").ToLowerInvariant();
        Verb superSpeed = new()
        {
            Text = superSpeedName,
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
            Message = string.Join(": ", superSpeedName, Loc.GetString("admin-smite-super-speed-description"))
        };
        args.Verbs.Add(superSpeed);

        //Bonk
        var superBonkLiteName = Loc.GetString("admin-smite-super-bonk-lite-name").ToLowerInvariant();
        Verb superBonkLite = new()
        {
            Text = superBonkLiteName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Structures/Furniture/Tables/glass.rsi"), "full"),
            Act = () =>
            {
                _superBonkSystem.StartSuperBonk(args.Target, stopWhenDead: true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superBonkLiteName, Loc.GetString("admin-smite-super-bonk-lite-description"))
        };
        args.Verbs.Add(superBonkLite);

        var superBonkName = Loc.GetString("admin-smite-super-bonk-name").ToLowerInvariant();
        Verb superBonk = new()
        {
            Text = superBonkName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Structures/Furniture/Tables/generic.rsi"), "full"),
            Act = () =>
            {
                _superBonkSystem.StartSuperBonk(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superBonkName, Loc.GetString("admin-smite-super-bonk-description"))
        };
        args.Verbs.Add(superBonk);

        var superslipName = Loc.GetString("admin-smite-super-slip-name").ToLowerInvariant();
        Verb superslip = new()
        {
            Text = superslipName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Objects/Specific/Janitorial/soap.rsi"), "omega-4"),
            Act = () =>
            {
                var hadSlipComponent = EnsureComp(args.Target, out SlipperyComponent slipComponent);
                if (!hadSlipComponent)
                {
                    slipComponent.SlipData.SuperSlippery = true;
                    slipComponent.SlipData.StunTime = TimeSpan.FromSeconds(5);
                    slipComponent.SlipData.LaunchForwardsMultiplier = 20;
                }

                _slipperySystem.TrySlip(args.Target, slipComponent, args.Target, requiresContact: false);
                if (!hadSlipComponent)
                {
                    RemComp(args.Target, slipComponent);
                }
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superslipName, Loc.GetString("admin-smite-super-slip-description"))
        };
        args.Verbs.Add(superslip);

        var omniaccentName = Loc.GetString("admin-smite-omni-accent-name").ToLowerInvariant();
        Verb omniaccent = new()
        {
            Text = omniaccentName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Interface/Actions/voice-mask.rsi"), "icon"),
            Act = () =>
            {
                EnsureComp<BarkAccentComponent>(args.Target);
                EnsureComp<BleatingAccentComponent>(args.Target);
                EnsureComp<FrenchAccentComponent>(args.Target);
                EnsureComp<GermanAccentComponent>(args.Target);
                EnsureComp<LizardAccentComponent>(args.Target);
                EnsureComp<MobsterAccentComponent>(args.Target);
                EnsureComp<MothAccentComponent>(args.Target);
                EnsureComp<OwOAccentComponent>(args.Target);
                EnsureComp<SkeletonAccentComponent>(args.Target);
                EnsureComp<SouthernAccentComponent>(args.Target);
                EnsureComp<SpanishAccentComponent>(args.Target);
                EnsureComp<StutteringAccentComponent>(args.Target);

                if (_random.Next(0, 8) == 0)
                {
                    EnsureComp<BackwardsAccentComponent>(args.Target); // was asked to make this at a low chance idk
                }
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", omniaccentName, Loc.GetString("admin-smite-omni-accent-description"))
        };
        args.Verbs.Add(omniaccent);

        var crawlerName = Loc.GetString("admin-smite-crawler-name").ToLowerInvariant();
        Verb crawler = new()
        {
            Text = crawlerName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Mobs/Animals/snake.rsi"), "icon"),
            Act = () =>
            {
                EnsureComp<WormComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", crawlerName, Loc.GetString("admin-smite-crawler-description"))
        };
        args.Verbs.Add(crawler);
    }
}
