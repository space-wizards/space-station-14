using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;

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
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CPRComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        SubscribeLocalEvent<CPRComponent, CPRDoAfterEvent>(OnDoCPREvent);
    }
    public void OnDoCPREvent(Entity<CPRComponent> ent, ref CPRDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (!TryComp<DamageableComponent>(ent, out var damage) || !TryComp<CPRComponent>(ent, out var cpr) || !TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return;
        DoLunge(args.User);
        if (_net.IsServer)
        {
            _audio.PlayPvs(cpr.Sound, ent.Owner);
        }
        _damage.TryChangeDamage(ent, cpr.Heal, interruptsDoAfters: false, damageable: damage, ignoreResistances: true);
        _damage.TryChangeDamage(ent, cpr.Damage, interruptsDoAfters: false, damageable: damage, ignoreResistances: true);

        args.Repeat = thresholds.CurrentThresholdState == MobState.Critical;
        args.Handled = true;
    }

    public abstract void DoLunge(EntityUid user);

    private void OnGetExamineVerbs(EntityUid uid, CPRComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage) || !TryComp<CPRComponent>(uid, out var cpr) || !TryComp<MobThresholdsComponent>(uid, out var thresholds) || !TryComp<MobThresholdsComponent>(args.User, out var myThresholds))
            return;

        if (thresholds.CurrentThresholdState != MobState.Critical)
            return;


        // low interaction range
        var inRange = _interactionSystem.InRangeUnobstructed(args.User, args.Target, SharedInteractionSystem.InteractionRange * 0.25f);

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            TimeSpan.FromSeconds(0.7f),
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

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                if (_doAfter.TryStartDoAfter(doAfterEventArgs) && _net.IsServer)
                {
                    _popup.PopupEntity(Loc.GetString("cpr-start", ("person", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(uid, EntityManager))), uid, PopupType.Medium);
                }
            },
            Text = Loc.GetString("cpr-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !inRange || myThresholds.CurrentThresholdState == MobState.Alive,
            Message = inRange ? null : Loc.GetString("cpr-verb-text-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cpr.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }
}

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
