using System;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    /// <summary>
    /// Tries to fire a round of ammo out of the weapon.
    /// </summary>
    /// <param name="user">Entity that is operating the weapon, usually the player.</param>
    /// <param name="targetPos">Target position on the map to shoot at.</param>
    private void TryFire(EntityUid user, Vector2 targetPos)
    {
        if (!_entMan.TryGetComponent(user, out HandsComponent? hands) || hands.GetActiveHand?.Owner != Owner)
        {
            return;
        }

        if (!_entMan.TryGetComponent(user, out CombatModeComponent? combat) || !combat.IsInCombatMode)
        {
            return;
        }

        if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user)) return;

        var fireAttempt = new GunSystem.GunFireAttemptEvent(user, this);
        _entMan.EventBus.RaiseLocalEvent(Owner, fireAttempt);

        if (!fireAttempt.Cancelled)
        {
            return;
        }

        var curTime = _gameTiming.CurTime;
        var span = curTime - _lastFireTime;
        if (span.TotalSeconds < 1 / _barrel?.FireRate)
        {
            return;
        }

        _lastFireTime = curTime;

        if (ClumsyCheck && ClumsyDamage != null && ClumsyComponent.TryRollClumsy(user, ClumsyExplodeChance))
        {
            //Wound them
            EntitySystem.Get<DamageableSystem>().TryChangeDamage(user, ClumsyDamage);
            EntitySystem.Get<StunSystem>().TryParalyze(user, TimeSpan.FromSeconds(3f), true);

            // Apply salt to the wound ("Honk!")
            SoundSystem.Play(
                Filter.Pvs(Owner), _clumsyWeaponHandlingSound.GetSound(),
                _entMan.GetComponent<TransformComponent>(Owner).Coordinates, AudioParams.Default.WithMaxDistance(5));

            SoundSystem.Play(
                Filter.Pvs(Owner), _clumsyWeaponShotSound.GetSound(),
                _entMan.GetComponent<TransformComponent>(Owner).Coordinates, AudioParams.Default.WithMaxDistance(5));

            user.PopupMessage(Loc.GetString("server-ranged-weapon-component-try-fire-clumsy"));

            _entMan.DeleteEntity(Owner);
            return;
        }

        if (_canHotspot)
        {
            EntitySystem.Get<AtmosphereSystem>().HotspotExpose(_entMan.GetComponent<TransformComponent>(user).Coordinates, 700, 50);
        }

        _entMan.EventBus.RaiseLocalEvent(Owner, new GunSystem.GunFireEvent());
    }

    private void OnPumpExamine(EntityUid uid, PumpBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("pump-barrel-component-on-examine", ("caliber", component.Caliber)));
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
