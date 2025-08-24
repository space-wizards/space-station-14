using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem
{
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedCreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocutionSystem = default!;

    protected virtual void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        var explodeName = Loc.GetString("admin-smite-explode-name").ToLowerInvariant();
        Verb explode = new()
        {
            Text = explodeName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Act = () => SmiteExplodeVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", explodeName, Loc.GetString("admin-smite-explode-description")) // we do this so the description tells admins the Text to run it via console.
        };
        args.Verbs.Add(explode);

        var chessName = Loc.GetString("admin-smite-chess-dimension-name").ToLowerInvariant();
        Verb chess = new()
        {
            Text = chessName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Fun/Tabletop/chessboard.rsi"), "chessboard"),
            Act = () => SmiteChessVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", chessName, Loc.GetString("admin-smite-chess-dimension-description"))
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            var flamesName = Loc.GetString("admin-smite-set-alight-name").ToLowerInvariant();
            Verb flames = new()
            {
                Text = flamesName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Alerts/Fire/fire.png")),
                Act = () => SmiteSetAlightVerb(args.User, args.Target, flammable),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", flamesName, Loc.GetString("admin-smite-set-alight-description"))
            };
            args.Verbs.Add(flames);
        }

        var monkeyName = Loc.GetString("admin-smite-monkeyify-name").ToLowerInvariant();
        Verb monkey = new()
        {
            Text = monkeyName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Animals/monkey.rsi"), "monkey"),
            Act = () => PolymorphEntity(args.Target, "AdminMonkeySmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", monkeyName, Loc.GetString("admin-smite-monkeyify-description"))
        };
        args.Verbs.Add(monkey);

        var disposalBinName = Loc.GetString("admin-smite-garbage-can-name").ToLowerInvariant();
        Verb disposalBin = new()
        {
            Text = disposalBinName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Piping/disposal.rsi"), "disposal"),
            Act = () => PolymorphEntity(args.Target, "AdminDisposalsSmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", disposalBinName, Loc.GetString("admin-smite-garbage-can-description"))
        };
        args.Verbs.Add(disposalBin);

        if (TryComp<DamageableComponent>(args.Target, out var damageable) && HasComp<MobStateComponent>(args.Target))
        {
            var hardElectrocuteName = Loc.GetString("admin-smite-electrocute-name").ToLowerInvariant();
            Verb hardElectrocute = new()
            {
                Text = hardElectrocuteName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/yellow.rsi"), "icon"),
                Act = () =>
                {
                    int damageToDeal;
                    if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Critical, out var criticalThreshold))
                    {
                        // We can't crit them so try killing them.
                        if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Dead, out var deadThreshold))
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

                    _electrocutionSystem.TryDoElectrocution(args.Target,
                        null,
                        damageToDeal,
                        TimeSpan.FromSeconds(30),
                        refresh: true,
                        ignoreInsulation: true);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", hardElectrocuteName, Loc.GetString("admin-smite-electrocute-description"))
            };
            args.Verbs.Add(hardElectrocute);
        }

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            var creamPieName = Loc.GetString("admin-smite-creampie-name").ToLowerInvariant();
            Verb creamPie = new()
            {
                Text = creamPieName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Consumable/Food/Baked/pie.rsi"), "plain-slice"),
                Act = () => _creamPieSystem.SetCreamPied(args.Target, creamPied, true),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", creamPieName, Loc.GetString("admin-smite-creampie-description"))
            };
            args.Verbs.Add(creamPie);
        }

        if (TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
        {
            var bloodRemovalName = Loc.GetString("admin-smite-remove-blood-name").ToLowerInvariant();
            Verb bloodRemoval = new()
            {
                Text = bloodRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Fluids/tomato_splat.rsi"), "puddle-1"),
                Act = () =>
                {
                    _bloodstreamSystem.SpillAllSolutions((args.Target, bloodstream));
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-blood-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-blood-others", ("name", args.Target)),
                        xform.Coordinates,
                        Filter.PvsExcept(args.Target),
                        true,
                        PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", bloodRemovalName, Loc.GetString("admin-smite-remove-blood-description"))
            };
            args.Verbs.Add(bloodRemoval);
        }

        // I don't know what this comment means, but by golly, it stays.
        // bobby...
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            var vomitOrgansName = Loc.GetString("admin-smite-vomit-organs-name").ToLowerInvariant();
            Verb vomitOrgans = new()
            {
                Text = vomitOrgansName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Fluids/vomit_toxin.rsi"), "vomit_toxin-1"),
                Act = () => SmiteVomitOrgansVerb(args.Target, body),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", vomitOrgansName, Loc.GetString("admin-smite-vomit-organs-description"))
            };
            args.Verbs.Add(vomitOrgans);

            var handsRemovalName = Loc.GetString("admin-smite-remove-hands-name").ToLowerInvariant();
            Verb handsRemoval = new()
            {
                Text = handsRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/remove-hands.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)),
                        baseXform.Coordinates,
                        Filter.PvsExcept(args.Target),
                        true,
                        PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", handsRemovalName, Loc.GetString("admin-smite-remove-hands-description"))
            };
            args.Verbs.Add(handsRemoval);

            var handRemovalName = Loc.GetString("admin-smite-remove-hand-name").ToLowerInvariant();
            Verb handRemoval = new()
            {
                Text = handRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/remove-hand.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand, body))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                        break;
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)),
                        baseXform.Coordinates,
                        Filter.PvsExcept(args.Target),
                        true,
                        PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", handRemovalName, Loc.GetString("admin-smite-remove-hand-description"))
            };
            args.Verbs.Add(handRemoval);

            var stomachRemovalName = Loc.GetString("admin-smite-stomach-removal-name").ToLowerInvariant();
            Verb stomachRemoval = new()
            {
                Text = stomachRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Species/Human/organs.rsi"), "stomach"),
                Act = () =>
                {
                    foreach (var entity in _bodySystem.GetBodyOrganEntityComps<StomachComponent>((args.Target, body)))
                    {
                        QueueDel(entity.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-stomach-removal-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", stomachRemovalName, Loc.GetString("admin-smite-stomach-removal-description"))
            };
            args.Verbs.Add(stomachRemoval);

            var lungRemovalName = Loc.GetString("admin-smite-lung-removal-name").ToLowerInvariant();
            Verb lungRemoval = new()
            {
                Text = lungRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Species/Human/organs.rsi"), "lung-r"),
                Act = () => SmiteRemoveLungsVerb(args.Target, body),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", lungRemovalName, Loc.GetString("admin-smite-lung-removal-description"))
            };
            args.Verbs.Add(lungRemoval);
        }
    }

    protected virtual void PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
    {
    }

    protected virtual void SmiteExplodeVerb(EntityUid target)
    {
    }

    protected virtual void SmiteChessVerb(EntityUid target)
    {
    }

    protected virtual void SmiteSetAlightVerb(EntityUid user, EntityUid target, FlammableComponent flammable)
    {
    }

    protected virtual void SmiteVomitOrgansVerb(EntityUid target, BodyComponent body)
    {
    }

    protected virtual void SmiteRemoveLungsVerb(EntityUid target, BodyComponent body)
    {
    }
}
