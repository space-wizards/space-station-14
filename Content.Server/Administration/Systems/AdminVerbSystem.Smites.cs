using System.Threading;
using Content.Server.Administration.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GhostKick;
using Content.Server.Pointing.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Clumsy;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Tabletop.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedGodmodeSystem _sharedGodmodeSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SuperBonkSystem _superBonkSystem = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;


    private readonly EntProtoId _actionViewLawsProtoId = "ActionViewLaws";
    private readonly ProtoId<SiliconLawsetPrototype> _crewsimovLawset = "Crewsimov";

    private readonly EntProtoId _siliconMindRole = "MindRoleSiliconBrain";
    private const string SiliconLawBoundUserInterface = "SiliconLawBoundUserInterface";

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

    protected override void SmiteGhostKickVerb(ActorComponent actor)
    {
        _ghostKickManager.DoDisconnect(actor.PlayerSession.Channel, "Smitten.");
    }

    protected override void SmiteKillSignVerb(EntityUid target)
    {
        EnsureComp<KillSignComponent>(target);
    }

    protected override void SmiteMaidenVerb(EntityUid target)
    {
        _outfit.SetOutfit(target,
            "JanitorMaidGear",
            (_, clothing) =>
        {
            if (HasComp<ClothingComponent>(clothing))
                EnsureComp<UnremoveableComponent>(clothing);
            EnsureComp<ClumsyComponent>(target);
        });
    }

    protected override void SmiteAngerPointingArrowsVerb(EntityUid target)
    {
        EnsureComp<PointingArrowAngeringComponent>(target);
    }

    protected override void SmiteYoutubeSimulationVerb(EntityUid target)
    {
        EnsureComp<BufferingComponent>(target);
    }

    protected override void SmiteBackwardsAccentVerb(EntityUid target)
    {
        EnsureComp<BackwardsAccentComponent>(target);
    }

    protected override void SmiteSuperBonkLiteVerb(EntityUid target)
    {
        _superBonkSystem.StartSuperBonk(target, stopWhenDead: true);
    }

    protected override void SmiteSuperBonkVerb(EntityUid target)
    {
        _superBonkSystem.StartSuperBonk(target);
    }

    protected override void SmiteOmniAccentVerb(EntityUid target)
    {
        EnsureComp<BarkAccentComponent>(target);
        EnsureComp<BleatingAccentComponent>(target);
        EnsureComp<FrenchAccentComponent>(target);
        EnsureComp<GermanAccentComponent>(target);
        EnsureComp<LizardAccentComponent>(target);
        EnsureComp<MobsterAccentComponent>(target);
        EnsureComp<MothAccentComponent>(target);
        EnsureComp<OwOAccentComponent>(target);
        EnsureComp<SkeletonAccentComponent>(target);
        EnsureComp<SouthernAccentComponent>(target);
        EnsureComp<SpanishAccentComponent>(target);
        EnsureComp<StutteringAccentComponent>(target);

        if (_random.Next(0, 8) == 0)
            EnsureComp<BackwardsAccentComponent>(target); // was asked to make this at a low chance idk
    }

    protected override void SmiteSiliconLawsVerb(EntityUid target)
    {
        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(target);
        _uiSystem.SetUi((target, userInterfaceComp),
            SiliconLawsUiKey.Key,
            new InterfaceData(SiliconLawBoundUserInterface));

        if (!HasComp<SiliconLawBoundComponent>(target))
        {
            EnsureComp<SiliconLawBoundComponent>(target);
            _actionsSystem.AddAction(target, _actionViewLawsProtoId);
        }

        EnsureComp<SiliconLawProviderComponent>(target);
        _siliconLawSystem.SetLaws(_siliconLawSystem.GetLawset(_crewsimovLawset).Laws, target);

        if (_mindSystem.TryGetMind(target, out var mindId, out _))
            _roleSystem.MindAddRole(mindId, _siliconMindRole);

        _popupSystem.PopupEntity(Loc.GetString("admin-smite-silicon-laws-bound-self"),
            target,
            target,
            PopupType.LargeCaution);
    }
}
