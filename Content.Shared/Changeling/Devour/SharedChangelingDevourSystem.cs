using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Devour;

public abstract partial class SharedChangelingDevourSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingDevourComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
    }

    private void OnInit(EntityUid uid, ChangelingDevourComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.ChangelingDevourActionEntity, component.ChangelingDevourAction);
        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);
        if (identityStorage.OriginalIdentityComponent == null && TryComp<AppearanceComponent>(uid, out var appearance) && TryComp<VocalComponent>(uid, out var vocals))
        {
            StoredIdentityComponent ling = new()
            {
                IdentityAppearance = appearance,
                IdentityVocals = vocals
            };
            identityStorage.Identities?.Add(ling);
            identityStorage.OriginalIdentityComponent = ling;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ChangelingDevourComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentDevourEvent != null)
                continue;

            var consumeStatus = _doAfterSystem.GetStatus(comp.CurrentDevourEvent?.DoAfter.Id);
            var target = comp.CurrentDevourEvent?.Args.Target;
            var user = comp.CurrentDevourEvent?.User;

            if (consumeStatus.Equals(DoAfterStatus.Cancelled))
            {
                comp.CurrentDevourEvent = null;
                _audioSystem.Stop(comp.CurrentDevourSound);
                comp.CurrentDevourSound = null;
                continue;
            }
            if (curTime > comp.NextTick)
            {
                if (consumeStatus.Equals(DoAfterStatus.Running))
                {
                    ConsumeDamageTick(target, comp, user);
                }
                comp.NextTick = curTime + TimeSpan.FromSeconds(1f);
            }
        }
    }
    private void ConsumeDamageTick(EntityUid? target, ChangelingDevourComponent comp, EntityUid? user)
    {
        if (target == null)
            return;

        if (TryComp<DamageableComponent>(target, out var damage))
        {
            if (damage.DamagePerGroup.TryGetValue("Brute", out var val) && val < comp.DevourConsumeDamageCap)
            {
                _damageable.TryChangeDamage(target, comp.DamagePerTick, true, true, damage, user);
            }
        }
    }
    private void OnDevourAction(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourActionEvent args)
    {
        var target = args.Target;
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Target))
            return;
        args.Handled = true;
        if (HasComp<ChangelingHuskedCorpseComponent>(target))
        {
            _popupSystem.PopupClient("changeling-devour-failed-husk", args.Performer, args.Performer);
            return;
        }
        _popupSystem.PopupPredicted("changeling-devour-begin-windup", args.Performer, null, PopupType.MediumCaution);
        //TODO: check if the target has a resistance of higher than 10% Brute
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DevourWindupTime, new ChangelingDevourWindupDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
        });

    }
    private void OnDevourWindup(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourWindupDoAfterEvent args)
    {
        component.CurrentDevourEvent = new ChangelingDevourConsumeDoAfterEvent();

        _popupSystem.PopupPredicted("changeling-devour-begin-consume", args.User, null, PopupType.LargeCaution);
        var sound = _audioSystem.PlayPredicted(component.ConsumeTickNoise, uid, uid);
        component.CurrentDevourSound = sound?.Entity;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DevourConsumeTime, component.CurrentDevourEvent, uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
        });
    }
    private void OnDevourConsume(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        //TODO: move Devour complete into if dead and make a separate one on the RARE chance someone survives the devour (and doesn't break it)
        //TODO: Also loc all the shit
        _popupSystem.PopupPredicted("changeling-devour-consume-complete", args.User, null, PopupType.LargeCaution);
        var target = args.Target;
        if (target == null)
            return;
        if (_mobState.IsDead((EntityUid)target) && TryComp<BodyComponent>(target, out var body)
            && TryComp<AppearanceComponent>(target, out var appearance)
            && TryComp<VocalComponent>(target, out var vocals)
            && TryComp<ChangelingIdentityComponent>(args.User, out var identitystorage))
        {
            EnsureComp<ChangelingHuskedCorpseComponent>((EntityUid)target);
            identitystorage.Identities?.Add(new()
            {
                IdentityAppearance = appearance,
                IdentityVocals = vocals
            });
            foreach (var organ in _bodySystem.GetBodyOrgans(target, body))
            {
                _bodySystem.RemoveOrgan(organ.Id);
                _entityManager.QueueDeleteEntity(organ.Id);
            }
        }
    }
}
public sealed partial class ChangelingDevourActionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class ChangelingDevourWindupDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class ChangelingDevourConsumeDoAfterEvent : SimpleDoAfterEvent { }

[ByRefEvent]
public struct ChangelingDevourFailedOnHuskEvent
{
    public bool Handled;
    public ChangelingDevourFailedOnHuskEvent()
    {
        Handled = false;
    }
}
