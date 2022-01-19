using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Interaction.Components;
using Content.Server.Stunnable;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    /// <summary>
    /// Tries to fire a round of ammo out of the weapon.
    /// </summary>
    /// <param name="user">Entity that is operating the weapon, usually the player.</param>
    /// <param name="targetPos">Target position on the map to shoot at.</param>
    private void TryFire(EntityUid user, Vector2 targetPos, ServerRangedWeaponComponent gun)
    {
        if (!TryComp(user, out HandsComponent? hands) || hands.GetActiveHand?.Owner != gun.Owner)
        {
            return;
        }

        if (!TryComp(user, out CombatModeComponent? combat) || !combat.IsInCombatMode)
        {
            return;
        }

        if (!_blocker.CanInteract(user)) return;

        var fireAttempt = new GunFireAttemptEvent(user, gun);
        EntityManager.EventBus.RaiseLocalEvent(gun.Owner, fireAttempt);

        if (fireAttempt.Cancelled)
            return;

        var curTime = _gameTiming.CurTime;
        var span = curTime - gun.LastFireTime;
        if (span.TotalSeconds < 1 / _barrel?.FireRate)
        {
            return;
        }

        // TODO: Clumsy should be eventbus I think?

        gun.LastFireTime = curTime;
        var coordinates = Transform(gun.Owner).Coordinates;

        if (gun.ClumsyCheck && gun.ClumsyDamage != null && ClumsyComponent.TryRollClumsy(user, gun.ClumsyExplodeChance))
        {
            //Wound them
            _damageable.TryChangeDamage(user, gun.ClumsyDamage);
            _stun.TryParalyze(user, TimeSpan.FromSeconds(3f), true);

            // Apply salt to the wound ("Honk!")
            SoundSystem.Play(
                Filter.Pvs(Owner), gun._clumsyWeaponHandlingSound.GetSound(),
                coordinates, AudioParams.Default.WithMaxDistance(5));

            SoundSystem.Play(
                Filter.Pvs(Owner), gun._clumsyWeaponShotSound.GetSound(),
                coordinates, AudioParams.Default.WithMaxDistance(5));

            user.PopupMessage(Loc.GetString("server-ranged-weapon-component-try-fire-clumsy"));

            EntityManager.DeleteEntity(gun.Owner);
            return;
        }

        if (gun.CanHotspot)
        {
            _atmos.HotspotExpose(coordinates, 700, 50);
        }

        EntityManager.EventBus.RaiseLocalEvent(gun.Owner, new GunFireEvent());
    }

    private void OnMagazineExamine(EntityUid uid, ServerMagazineBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("server-magazine-barrel-component-on-examine", ("caliber", component.Caliber)));

        foreach (var magazineType in component.GetMagazineTypes())
        {
            args.PushMarkup(Loc.GetString("server-magazine-barrel-component-on-examine-magazine-type", ("magazineType", magazineType)));
        }
    }
}
