using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Bible.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SSDIndicator;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CosmicCult;

public sealed class DeconversionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CleanseOnUseComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CleanseOnUseComponent, CleanseOnDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CleanseCultComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<CleanseCultComponent> uid, ref ComponentInit args)
    {
        _entityManager.System<SharedJitteringSystem>().DoJitter(uid.Owner, uid.Comp.CleanseDuration, true, 5, 20);
        uid.Comp.CleanseTime = _timing.CurTime + uid.Comp.CleanseDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var deconCultTimer = EntityQueryEnumerator<CleanseCultComponent>();
        while (deconCultTimer.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.CleanseTime && !HasComp<CosmicMarkBlankComponent>(uid))
            {
                RemComp<CleanseCultComponent>(uid);
                DeconvertCultist(uid);
            }
        }
    }

    private void OnAfterInteract(Entity<CleanseOnUseComponent> uid, ref AfterInteractEvent args)
    {
        if (!TryComp(uid, out UseDelayComponent? useDelay) || _delay.IsDelayed((uid, useDelay)))
            return;
        if (!args.CanReach || !uid.Comp.Enabled || args.Target == null || _mobState.IsDead(args.Target.Value))
            return;

        if (!HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("cleanse-item-sizzle", ("target", Identity.Entity(args.Used, EntityManager))), args.User, args.User);

            _audio.PlayPvs(uid.Comp.SizzleSound, args.User);
            _damageable.TryChangeDamage(args.User, uid.Comp.SelfDamage, origin: uid);
            _delay.TryResetDelay((uid, useDelay));
            return;
        }

        _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-begin", ("target", Identity.Entity(args.User, EntityManager))), args.User, args.Target.Value);
        _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-begin-user", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.User, args.User);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.UseTime, new CleanseOnDoAfterEvent(), uid, args.Target, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1.5f,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(Entity<CleanseOnUseComponent> uid, ref CleanseOnDoAfterEvent args)
    {
        var target = args.Args.Target;
        if (!TryComp(uid, out UseDelayComponent? useDelay) || args.Cancelled || args.Handled || target == null || _mobState.IsDead(target.Value))
            return;
        var targetPosition = Transform(target.Value).Coordinates;
        //TODO: This could be made more agnostic, but there's only one cult for now, and frankly, i'm so tired. This is easy to read and easy to modify code. Expand it at thine leisure.
        if (TryComp<CosmicCultComponent>(args.Target, out var comp) && comp.CosmicEmpowered)
        {

            Spawn(uid.Comp.MalignVFX, targetPosition);
            Spawn(uid.Comp.MalignVFX, Transform(args.User).Coordinates);
            EnsureComp<CleanseCultComponent>(target.Value, out var cleanse);
            cleanse.CleanseDuration = TimeSpan.FromSeconds(1);
            _audio.PlayPvs(uid.Comp.MalignSound, targetPosition, AudioParams.Default.WithVolume(2f));
            _damageable.TryChangeDamage(args.User, uid.Comp.SelfDamage, true);
            _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-success-empowered", ("target", Identity.Entity(target.Value, EntityManager))), args.User, args.User);
        }
        else if (TryComp<CosmicCultComponent>(target, out var cultComponent) && !cultComponent.CosmicEmpowered)
        {
            Spawn(uid.Comp.CleanseVFX, targetPosition);
            EnsureComp<CleanseCultComponent>(target.Value, out var cleanse);
            cleanse.CleanseDuration = TimeSpan.FromSeconds(1);
            _audio.PlayPvs(uid.Comp.CleanseSound, targetPosition, AudioParams.Default.WithVolume(4f));
            _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-success", ("target", Identity.Entity(target.Value, EntityManager))), args.User, args.User);
        }
        else if (HasComp<RogueAscendedInfectionComponent>(target))
        {
            Spawn(uid.Comp.CleanseVFX, targetPosition);
            RemComp<RogueAscendedInfectionComponent>(target.Value);
            _audio.PlayPvs(uid.Comp.CleanseSound, targetPosition, AudioParams.Default.WithVolume(4f));
            _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-success", ("target", Identity.Entity(target.Value, EntityManager))), args.User, args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-notcorrupted", ("target", Identity.Entity(target.Value, EntityManager))), args.User, args.User);
        }
        _delay.TryResetDelay((uid, useDelay));
        args.Handled = true;
    }

    private void DeconvertCultist(EntityUid uid)
    {
        RemComp<CosmicCultComponent>(uid);
    }
}
