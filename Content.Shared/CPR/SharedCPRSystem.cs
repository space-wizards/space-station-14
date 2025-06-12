using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.CPR;
/// <summary>
/// Used for handling CPR on critical breathing mobs
/// </summary>
public abstract partial class SharedCPRSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly EntityManager Ent = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const float CPRInteractionRangeMultiplier = 0.25f;
    public const float CPRDoAfterDelay = 0.7f;
    public const float CPRAnimationLength = 0.2f;
    public const float CPRAnimationEndTime = 1f; // This is set to much higher than the actual animation length to avoid it stopping prematurely, as it did in testing. Shouldnt affect anything

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CPRComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<CPRComponent, CPRDoAfterEvent>(OnCPRDoAfter);
    }

    public void OnCPRDoAfter(Entity<CPRComponent> ent, ref CPRDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<DamageableComponent>(ent, out var damage) || !TryComp<CPRComponent>(ent, out var cpr) || !TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return;

        DoLunge(args.User);

        _audio.PlayPredicted(cpr.Sound, ent.Owner, args.User);
        _damage.TryChangeDamage(ent, cpr.Change, interruptsDoAfters: false, damageable: damage, ignoreResistances: true);

        // burst of oxygen when not critical anymore
        if (thresholds.CurrentThresholdState != MobState.Critical)
        {
            _damage.TryChangeDamage(ent, cpr.BonusHeal, interruptsDoAfters: false, damageable: damage, ignoreResistances: true);
        }

        args.Repeat = thresholds.CurrentThresholdState == MobState.Critical;
        args.Handled = true;
    }
    /// <summary>
    /// Makes a user do the CPR Lunge animation
    /// </summary>
    /// <param name="user">The entity to animate</param>
    public abstract void DoLunge(EntityUid user);

    private void OnGetVerbs(EntityUid uid, CPRComponent component, GetVerbsEvent<Verb> args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage) || !TryComp<CPRComponent>(uid, out var cpr) || !TryComp<MobThresholdsComponent>(uid, out var thresholds) || !TryComp<MobThresholdsComponent>(args.User, out var myThresholds))
            return;

        if (thresholds.CurrentThresholdState != MobState.Critical)
            return;


        // low interaction range
        var inRange = _interactionSystem.InRangeUnobstructed(args.User, args.Target, SharedInteractionSystem.InteractionRange * CPRInteractionRangeMultiplier);

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            TimeSpan.FromSeconds(CPRDoAfterDelay),
            new CPRDoAfterEvent(),
            args.Target,
            args.User
            )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DamageThreshold = 5,
            BlockDuplicate = true,
            RequireCanInteract = true
        };

        var verb = new Verb()
        {
            Act = () =>
            {
                _doAfter.TryStartDoAfter(doAfterEventArgs);
                _popup.PopupPredicted(Loc.GetString("cpr-start-you", ("target", Identity.Entity(uid, EntityManager))), Loc.GetString("cpr-start", ("person", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(uid, EntityManager))), args.User, args.User, PopupType.Medium);
            },
            Text = Loc.GetString("cpr-verb-text"),
            Disabled = !inRange || myThresholds.CurrentThresholdState == MobState.Alive,
            Message = inRange ? null : Loc.GetString("cpr-verb-text-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cpr.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }
}

/// <summary>
/// Do-after event used for CPR
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
