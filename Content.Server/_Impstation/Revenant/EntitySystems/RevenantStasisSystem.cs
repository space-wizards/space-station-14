using Content.Server.Bible;
using Content.Server.Bible.Components;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Kitchen.EntitySystems;
using Content.Server.Mind;
using Content.Server.Revenant.Components;
using Content.Server.Speech.Components;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Revenant;
using Content.Shared.Speech;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantStasisSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRoles = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string RevenantStasisId = "Stasis";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantStasisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantStasisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RevenantStasisComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<RevenantStasisComponent, ChangeDirectionAttemptEvent>(OnAttemptDirection);
        SubscribeLocalEvent<RevenantStasisComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RevenantStasisComponent, ConstructionConsumedObjectEvent>(OnCrafted);
        SubscribeLocalEvent<RevenantStasisComponent, GrindAttemptEvent>(OnGrindAttempt);
        SubscribeLocalEvent<RevenantStasisComponent, TransformSpeakerNameEvent>(OnTransformName);

        SubscribeLocalEvent<RevenantStasisComponent, AfterInteractUsingEvent>(OnBibleInteract, before: [typeof(BibleSystem)]);
        SubscribeLocalEvent<RevenantStasisComponent, ExorciseRevenantDoAfterEvent>(OnExorcise);
    }

    private void OnStartup(EntityUid uid, RevenantStasisComponent component, ComponentStartup args)
    {
        EnsureComp<AlertsComponent>(uid);

        EnsureComp<StatusEffectsComponent>(uid);
        _statusEffects.TryAddStatusEffect(uid, RevenantStasisId, component.StasisDuration, true);

        var mover = EnsureComp<InputMoverComponent>(uid);
        mover.CanMove = false;
        Dirty(uid, mover);

        var speech = EnsureComp<SpeechComponent>(uid);
        speech.SpeechVerb = "Ghost";
        Dirty(uid, speech);

        if (TryComp<GhostRoleComponent>(uid, out var ghostRole))
            _ghostRoles.UnregisterGhostRole((uid, ghostRole));
    }

    private void OnTransformName(EntityUid uid, RevenantStasisComponent comp, TransformSpeakerNameEvent args)
    {
        args.VoiceName = Name(comp.Revenant);
        args.SpeechVerb = "Ghost";
    }

    private void OnShutdown(EntityUid uid, RevenantStasisComponent component, ComponentShutdown args)
    {
        if (_statusEffects.HasStatusEffect(uid, RevenantStasisId))
        {
            if (_mind.TryGetMind(uid, out var mindId, out var _))
                _mind.TransferTo(mindId, null);
            QueueDel(component.Revenant);
        }
    }

    private void OnStatusEnded(EntityUid uid, RevenantStasisComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stasis")
        {
            _transformSystem.SetCoordinates(component.Revenant, Transform(uid).Coordinates);
            _transformSystem.AttachToGridOrMap(component.Revenant);
            _meta.SetEntityPaused(component.Revenant, false);
            if (_mind.TryGetMind(uid, out var mindId, out var _))
                _mind.TransferTo(mindId, component.Revenant);
            QueueDel(uid);
        }
    }

    private void OnExamine(Entity<RevenantStasisComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("revenant-stasis-regenerating"));
    }

    private void OnCrafted(EntityUid uid, RevenantStasisComponent comp, ConstructionConsumedObjectEvent args)
    {
        // Permanently sealed into revenant plushie
        EnsureComp<InputMoverComponent>(args.New);

        var voice = EnsureComp<VoiceOverrideComponent>(args.New);
        voice.SpeechVerbOverride = "Ghost";
        voice.NameOverride = Name(comp.Revenant);

        if (_mind.TryGetMind(uid, out var mindId, out var _))
            _mind.TransferTo(mindId, args.New);
    }

    private void OnGrindAttempt(EntityUid uid, RevenantStasisComponent comp, GrindAttemptEvent args)
    {
        if (!comp.Revenant.Comp.GrindingRequiresSalt)
            return;

        foreach (var reagent in args.Reagents)
        {
            if (_tags.HasAnyTag(reagent, "Salt", "Holy"))
                return;
        }

        // Ripped off the changeling blood explosion variables
        _explosion.QueueExplosion(
            args.Grinder.Owner,
            "Default",
            7.5f, // totalIntensity
            4f, // slope
            2f // maxTileIntensity
        );

        args.Cancel();
    }

    private void OnAttemptDirection(EntityUid uid, RevenantStasisComponent comp, ChangeDirectionAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnBibleInteract(EntityUid uid, RevenantStasisComponent comp, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;
        if (args.Target == null)
            return;
        var bible = args.Used;
        var target = args.Target.Value;
        var user = args.User;
        if (!HasComp<BibleComponent>(args.Used))
            return;

        if (!TryComp<RevenantStasisComponent>(target, out var stasis))
            return;

        var revenant = stasis.Revenant;

        if (revenant.Comp.ExorcismRequiresBibleUser && !HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("revenant-exorcise-fail", ("bible", bible)), user, user);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(10), new ExorciseRevenantDoAfterEvent(), target, target, bible)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 1f
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("revenant-exorcise-begin-user", [("bible", bible), ("user", user), ("revenant", revenant.Owner)]), user, user);
        _popup.PopupEntity(Loc.GetString("revenant-exorcise-begin-target", [("bible", bible), ("user", user), ("revenant", revenant.Owner)]), target, target, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("revenant-exorcise-begin-other", [("bible", bible), ("user", user), ("revenant", revenant.Owner)]), target, Filter.Pvs(target).RemovePlayersByAttachedEntity([user, target]), true);
    }

    private void OnExorcise(EntityUid uid, RevenantStasisComponent comp, ExorciseRevenantDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target == null || args.Used == null)
            return;

        var target = args.Target.Value;
        var used = args.Used.Value;

        _popup.PopupEntity(Loc.GetString("revenant-exorcise-success", [("bible", used), ("user", args.User), ("revenant", comp.Revenant.Owner)]), target);

        RemComp<RevenantStasisComponent>(args.Target.Value);
    }
}
