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
using Content.Shared.Damage.Prototypes;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NameModifier.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Wagging;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingDevourComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
        SubscribeLocalEvent<ChangelingDevourComponent, DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent>>(OnConsumeAttemptTick);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ChangelingDevourComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            //Clean up canceled Sounds for Changelings if they cancel them
            var consumeStatus = _doAfterSystem.GetStatus(comp.CurrentDevourEvent?.DoAfter.Id);
            if (!consumeStatus.Equals(DoAfterStatus.Cancelled))
                continue;
            comp.CurrentWindupEvent = null;
            comp.CurrentDevourEvent = null;
            _audioSystem.Stop(comp.CurrentDevourSound);
            comp.CurrentDevourSound = null;
            Dirty(uid, comp);
        }
    }

    private void OnConsumeAttemptTick(EntityUid uid,
        ChangelingDevourComponent component,
        DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent> eventData)
    {
        if (component.CurrentDevourEvent == null)
            return;
        if (component.CurrentWindupEvent != null)
            return;

        var target = component.CurrentDevourEvent?.Args.Target;
        var user = component.CurrentDevourEvent?.User;
        var curTime = _timing.CurTime;
        if (curTime > component.NextTick)
        {
            ConsumeDamageTick(target, component, user);
            component.NextTick = curTime + TimeSpan.FromSeconds(1f);
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
    private void OnInit(EntityUid uid, ChangelingDevourComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.ChangelingDevourActionEntity, component.ChangelingDevourAction);
        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);
        if (identityStorage.OriginalIdentityComponent == null
            && TryComp<HumanoidAppearanceComponent>(uid, out var appearance)
            && TryComp<VocalComponent>(uid, out var vocals)
            && TryComp<DnaComponent>(uid, out var dna))
        {
            StoredIdentityComponent ling = new()
            {
                IdentityDna = dna,
                IdentityAppearance = appearance,
                IdentityVocals = vocals
            };
            identityStorage.Identities?.Add(ling);
            identityStorage.OriginalIdentityComponent = ling;
        }
    }

    private void OnDevourAction(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourActionEvent args)
    {
        Dirty(args.Performer, component);

        if (component.CurrentDevourEvent != null
            || component.CurrentWindupEvent != null)
            return;
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Target))
            return;
        if (!TryComp<DamageableComponent>(args.Target, out var damageable))
            return;

        var curTime = _timing.CurTime;
        component.NextTick = curTime + TimeSpan.FromSeconds(15); // Prevent someone from spamming and canceling the Action repeatedly and doing absurd damage due to DoAfter's continuing onwards on cancel
        args.Handled = true;
        component.CurrentWindupEvent = new ChangelingDevourWindupDoAfterEvent();
        var target = args.Target;

        //TODO: check if the target has a resistance of higher than 10% Brute
       if (_prototypeManager.TryIndex(damageable.DamageModifierSetId, out var prototype))
       {
           if(prototype.Coefficients.TryGetValue("Slash", out var slash) && slash > 0.1
              || prototype.Coefficients.TryGetValue("Blunt", out var blunt) && blunt > 0.1
              || prototype.Coefficients.TryGetValue("Piercing", out var peirce) && peirce > 0.1)
           {
               _popupSystem.PopupClient(Loc.GetString("changeling-devour-failed-armored"),
                   args.Performer,
                   args.Performer);
               return;
           }
       }


       if (HasComp<ChangelingHuskedCorpseComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-failed-husk"), args.Performer, args.Performer);
            return;
        }
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DevourWindupTime, component.CurrentWindupEvent, uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
        });

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-windup"), args.Performer, null, PopupType.MediumCaution);

    }
    private void OnDevourWindup(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourWindupDoAfterEvent args)
    {
        var curTime = _timing.CurTime;
        args.Handled = true;


        if(component.CurrentWindupEvent == null)
            return;


        Dirty(args.User, component);
        var windupStatus = _doAfterSystem.GetStatus(args.DoAfter.Id);
        if(!windupStatus.Equals(DoAfterStatus.Cancelled))
        {
            component.CurrentDevourEvent = new ChangelingDevourConsumeDoAfterEvent();
            _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-consume"),
                args.User,
                null,
                PopupType.LargeCaution);
            var sound = _audioSystem.PlayPredicted(component.ConsumeTickNoise, uid, uid);
            component.CurrentDevourSound = sound?.Entity;
            component.NextTick = curTime + TimeSpan.FromSeconds(1);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
                uid,
                component.DevourConsumeTime,
                component.CurrentDevourEvent,
                uid,
                target: args.Target,
                used: uid)
            {
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnMove = true,
                BlockDuplicate = true,
            });
        }
        component.CurrentWindupEvent = null;
    }
    private void OnDevourConsume(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        //TODO: move Devour complete into if dead and make a separate one on the RARE chance someone survives the devour (and doesn't break it)
        //TODO: Make Consumption conditional on rotted bodies (I.E Fails with a unique popup)

        var target = args.Target;
        if(component.CurrentDevourEvent == null)
            return;
        if (target == null)
            return;

        var consumeStatus = _doAfterSystem.GetStatus(component.CurrentDevourEvent?.DoAfter.Id);
        if (consumeStatus != DoAfterStatus.Cancelled)
        {
            _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-consume-complete"), args.User, null, PopupType.LargeCaution);
        }

        if (_mobState.IsDead((EntityUid)target)
            && TryComp<BodyComponent>(target, out var body)
            && TryComp<HumanoidAppearanceComponent>(target, out var appearance)
            && TryComp<VocalComponent>(target, out var vocals)
            && TryComp<ChangelingIdentityComponent>(args.User, out var identitystorage)
            && TryComp<DnaComponent>(target, out var dna))
        {

            EnsureComp<ChangelingHuskedCorpseComponent>((EntityUid)target);
            var name = Name((EntityUid)target);
            if (TryComp<NameModifierComponent>(target, out var namemodifier))
                name = namemodifier.BaseName;
            var description = MetaData((EntityUid)target).EntityDescription;

            var consumedIdentity = new StoredIdentityComponent()
            {
                IdentityName = name,
                IdentityDescription = description,
                IdentityDna = dna,
                IdentityAppearance = appearance,
                IdentityVocals = vocals,
                IdentityEntityPrototype = Prototype((EntityUid)target),
            };
            identitystorage.Identities?.Add(consumedIdentity);
            identitystorage.LastConsumedIdentityComponent = consumedIdentity;
            foreach (var organ in _bodySystem.GetBodyOrgans(target, body))
            {
                _bodySystem.RemoveOrgan(organ.Id);
                _entityManager.QueueDeleteEntity(organ.Id);
            }
        }
        component.CurrentDevourEvent = null;
        Dirty(uid, component);
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
