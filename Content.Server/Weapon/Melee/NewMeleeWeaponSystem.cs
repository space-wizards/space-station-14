using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Damage.Systems;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapon.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;

    public const float DamagePitchVariation = 0.05f;

    // TODO:
    // - Sprite lerping -> Check rotated eyes
    // - Eye kick?
    // - Arc
    // - Wide attack
    // - Better overlay
    // - Port
    // - CVars to toggle some stuff

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Pvs(uid.Value, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    protected override void DoPreciseAttack(EntityUid user, ReleasePreciseAttackEvent ev, NewMeleeWeaponComponent component)
    {
        base.DoPreciseAttack(user, ev, component);

        // Can't attack yourself
        // Not in LOS.
        if (user == ev.Target ||
            Deleted(ev.Target) ||
            !TryComp<TransformComponent>(ev.Target, out var targetXform) ||
            !_interaction.InRangeUnobstructed(user, ev.Target))
        {
            return;
        }

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(new List<EntityUid>() { ev.Target }, user, component.Damage);
        RaiseLocalEvent(component.Owner, hitEvent);

        if (hitEvent.Handled)
            return;

        var targets = new List<EntityUid>(1)
        {
            ev.Target
        };

        // For stuff that cares about it being attacked.
        RaiseLocalEvent(ev.Target, new AttackedEvent(component.Owner, user, targetXform.Coordinates));

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(component.Damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var damageResult = _damageable.TryChangeDamage(ev.Target, modifiedDamage);

        if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
        {
            // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
            if (damageResult.DamageDict.TryGetValue("Blunt", out var bluntDamage))
            {
                _stamina.TakeStaminaDamage(ev.Target, (bluntDamage * component.BluntStaminaDamageFactor).Float());
            }

            if (component.Owner == user)
                _adminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target):target} using their hands and dealt {damageResult.Total:damage} damage");
            else
                _adminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target):target} using {ToPrettyString(component.Owner):used} and dealt {damageResult.Total:damage} damage");

            PlayHitSound(ev.Target, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
        }
        else
        {
            if (hitEvent.HitSoundOverride != null)
            {
                Audio.PlayPvs(hitEvent.HitSoundOverride, component.Owner);
            }
            else
            {
                Audio.PlayPvs(component.NoDamageSound, component.Owner);
            }
        }

        if (damageResult != null)
        {
            RaiseNetworkEvent(new MeleeEffectEvent(targets), Filter.Pvs(targetXform.Coordinates, entityMan: EntityManager));
        }
    }

    protected override void DoLunge(EntityUid user, Vector2 localPos, string? animation)
    {
        RaiseNetworkEvent(new MeleeLungeEvent(user, localPos, animation), Filter.Pvs(user, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    private void PlayHitSound(EntityUid target, string? type, SoundSpecifier? hitSoundOverride, SoundSpecifier? hitSound)
    {
        var playedSound = false;

        // Play sound based off of highest damage type.
        if (TryComp<MeleeSoundComponent>(target, out var damageSoundComp))
        {
            if (type == null && damageSoundComp.NoDamageSound != null)
            {
                Audio.PlayPvs(damageSoundComp.NoDamageSound, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayPvs(damageSoundType, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayPvs(damageSoundGroup, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Use weapon sounds if the thing being hit doesn't specify its own sounds.
        if (!playedSound)
        {
            if (hitSoundOverride != null)
            {
                Audio.PlayPvs(hitSoundOverride, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (hitSound != null)
            {
                Audio.PlayPvs(hitSound, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Fallback to generic sounds.
        if (!playedSound)
        {
            switch (type)
            {
                // Unfortunately heat returns caustic group so can't just use the damagegroup in that instance.
                case "Burn":
                case "Heat":
                case "Cold":
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                // No damage, fallback to tappies
                case null:
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/tap.ogg"), target, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                case "Brute":
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/smash.ogg"), target, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
            }
        }
    }

    public static string? GetHighestDamageSound(DamageSpecifier modifiedDamage, IPrototypeManager protoManager)
    {
        var groups = modifiedDamage.GetDamagePerGroup(protoManager);

        // Use group if it's exclusive, otherwise fall back to type.
        if (groups.Count == 1)
        {
            return groups.Keys.First();
        }

        var highestDamage = FixedPoint2.Zero;
        string? highestDamageType = null;

        foreach (var (type, damage) in modifiedDamage.DamageDict)
        {
            if (damage <= highestDamage) continue;
            highestDamageType = type;
        }

        return highestDamageType;
    }
}
