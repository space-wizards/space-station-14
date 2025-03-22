using Content.Shared.Popups;
using Content.Shared.Radiation.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Traits.Assorted;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Radiation.Systems;

public sealed class RadiationNoticingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnIrradiated(EntityUid uid, ActorComponent actorComponent, OnIrradiatedEvent args)
    {
        // Convert recieved radiation from event into actual radiation after rad damage reductions
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return; // If you aren't taking damage from radiation then what the messages say make no sense

        var trueTotalRads = args.TotalRads;

        DamageSpecifier damage = new();
        foreach (var typeId in damageable.RadiationDamageTypeIDs)
        {
            damage.DamageDict.Add(typeId, FixedPoint2.New(args.TotalRads));
        }

        // Calculate entity inherent resistances
        if (damageable.DamageModifierSetId != null &&
                    _prototypeManager.TryIndex<DamageModifierSetPrototype>(damageable.DamageModifierSetId, out var modifierSet))
        {
            // Hook into damage modifier calculation system
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
        }

        //Raise event to get all armor/shield/etc resistances
        var ev = new DamageModifyEvent(damage, null, true);
        RaiseLocalEvent(uid, ev);
        damage = ev.Damage;

        if (damage.Empty)
        {
            return;
        }
        trueTotalRads = (float)damage.GetTotal();

        // Roll chance for popup messages
        // Based on incoming rads so variable frame time does not alter message rate
        // Tuned for approx 1 message every 5 seconds or so per 1 rad/second incoming
        if (_random.NextFloat() <= trueTotalRads / 14)
        {
            SendRadiationPopup(uid);
        }

        //TODO: Expand system with other effects: visual spots, vomiting blood?, blurry vision?
    }

    private void SendRadiationPopup(EntityUid uid){
        List<string> msgArr = [
                "radiation-noticing-message-0",
                "radiation-noticing-message-1",
                "radiation-noticing-message-2",
                "radiation-noticing-message-3",
                "radiation-noticing-message-pain-0",
                "radiation-noticing-message-pain-1",
                "radiation-noticing-message-pain-2",
                "radiation-noticing-message-pain-3"
            ];

        // Todo: detect possessing specific types of organs/blood/etc and conditionally add related messages to the list

        // pick a random message
        var msgId = _random.Pick(msgArr);
        var msg = Loc.GetString(msgId);

        if (HasComp<PainNumbnessComponent>(uid) && msgId.Contains("-pain-"))
            return; // Do not show pain messages if the person has pain numbness

        // show it as a popup
        _popupSystem.PopupEntity(msg, uid, uid);
    }

}
