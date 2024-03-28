using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounding.Components;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{
    private void InitDamage()
    {
        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnWoundableDamaged);
        SubscribeLocalEvent<BodyComponent, DamageChangedEvent>(OnBodyDamaged);
    }


     private void OnBodyDamaged(EntityUid bodyEnt, BodyComponent body, ref DamageChangedEvent args)
    {
        //TODO: Make this method not MEGA ASS, because jesus christ I'm giving myself terminal space aids by doing this.
        //This is all placeholder and terrible, rewrite asap

        //Do not handle damage if it is being set instead of being changed.
        //We will handle that with another listener
        if (args.DamageDelta == null)
            return;
        if (!_bodySystem.TryGetRootBodyPart(bodyEnt, out var rootPart, body))
            return;

        //TODO: This is a quick hack to prevent asphyxiation/bloodloss from damaging bodyparts
        //Once proper body/organ simulation is implemented these can be removed
        args.DamageDelta.DamageDict.Remove("Asphyxiation");
        args.DamageDelta.DamageDict.Remove("Bloodloss");
        args.DamageDelta.DamageDict.Remove("Structural");
        if (args.DamageDelta.Empty)
            return;

        DamageableComponent? damagableComp;

        if (_random.NextFloat(0f, 1f) > NonCoreDamageChance)
        {
            var heads = _bodySystem.GetBodyChildrenOfType(bodyEnt, BodyPartType.Head, body).ToList();
            if (_random.NextFloat(0f, 1f) <= HeadDamageChance && heads.Count > 0)
            {

                var (headId, _) = heads[_random.Next(heads.Count)];
                if (TryComp(headId, out damagableComp))
                {
                    _damageableSystem.TryChangeDamage(headId, args.DamageDelta, damageable: damagableComp);
                    return;
                }
            }
            if (TryComp(rootPart, out damagableComp))
            {
                _damageableSystem.TryChangeDamage(rootPart.Value, args.DamageDelta, damageable: damagableComp);
                return;
            }
        }
        var children = _bodySystem.GetBodyPartDirectChildren(rootPart.Value, rootPart.Value.Comp).ToArray();
        Entity<BodyPartComponent> foundTarget = children[_random.Next(0, children.Length)];
        while (_random.NextFloat(0, 1f) > ChanceForPartSelection)
        {
            children = _bodySystem.GetBodyPartDirectChildren(foundTarget, foundTarget.Comp).ToArray();
            if (children.Length == 0)
                break;
            foundTarget = children[_random.Next(0, children.Length)];
        }
        _damageableSystem.TryChangeDamage(foundTarget, args.DamageDelta);
    }

    private void OnWoundableDamaged(EntityUid owner, WoundableComponent woundableComp, ref DamageChangedEvent args)
    {
        //Do not handle damage if it is being set instead of being changed.
        //We will handle that with another listener
        if (args.DamageDelta == null)
            return;
        CreateWoundsFromDamage(new Entity<WoundableComponent?>(owner, woundableComp), args.DamageDelta);
        ApplyDamageToWoundable(new Entity<WoundableComponent>(owner, woundableComp), args.DamageDelta);
    }

    private void ApplyDamageToWoundable(Entity<WoundableComponent> woundable, DamageSpecifier damageSpec)
    {
        woundable.Comp.Health -= damageSpec.GetTotal();
        Dirty(woundable);
        if (woundable.Comp.Health > woundable.Comp.MaxHealth)
        {
            woundable.Comp.Health = woundable.Comp.MaxHealth;
            return;
        }
        ValidateWoundable(woundable);
    }

    private void ValidateWoundable(Entity<WoundableComponent> woundable)
    {
        var dirty = false;

        if (woundable.Comp.Health > woundable.Comp.MaxHealth)
        {
            woundable.Comp.Health = woundable.Comp.MaxHealth;
            dirty = true;
        }

        if (woundable.Comp.Health < 0)
        {
            woundable.Comp.Integrity += woundable.Comp.Health;
            woundable.Comp.Health = 0;
            dirty = true;
        }

        if (woundable.Comp.IntegrityCap > woundable.Comp.MaxIntegrity)
        {
            woundable.Comp.IntegrityCap = woundable.Comp.MaxIntegrity;
            dirty = true;
        }

        if (woundable.Comp.Integrity > woundable.Comp.IntegrityCap)
        {
            woundable.Comp.Integrity = woundable.Comp.IntegrityCap;
            dirty = true;
        }
        if (dirty)
            Dirty(woundable);
        if (_netManager.IsClient)
            return;
        if (woundable.Comp.Integrity <= 0)
            TryGibWoundable(woundable);
    }


    private bool TryGibWoundable(Entity<WoundableComponent> woundable)
    {

        if (woundable.Comp.Integrity > 0)
            return false;

        //TODO: gib woundable. Setting int to 0 is placeholder until partloss is implemented
        woundable.Comp.Integrity = 0;
        Log.Debug($"{ToPrettyString(woundable.Owner)} is at 0 integrity and should have been destroyed (Part Gibbing not implemented yet).");
        return true;
    }
}
