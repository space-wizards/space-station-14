using System;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    private void OnAmmoExamine(EntityUid uid, AmmoComponent component, ExaminedEvent args)
    {
        var text = Loc.GetString("ammo-component-on-examine",("caliber", component.Caliber));
        args.PushMarkup(text);
    }

    public EntityUid? TakeBullet(AmmoComponent component, EntityCoordinates spawnAt)
    {
        if (component.AmmoIsProjectile)
        {
            return component.Owner;
        }

        if (component.Spent)
        {
            return null;
        }

        component.Spent = true;

        if (TryComp(component.Owner, out AppearanceComponent? appearanceComponent))
        {
            appearanceComponent.SetData(AmmoVisuals.Spent, true);
        }

        var entity = EntityManager.SpawnEntity(component.ProjectileId, spawnAt);

        return entity;
    }

    public void MuzzleFlash(EntityUid entity, AmmoComponent component, Angle angle)
    {
        if (string.IsNullOrEmpty(component.MuzzleFlashSprite))
        {
            return;
        }

        var time = _gameTiming.CurTime;
        var deathTime = time + TimeSpan.FromMilliseconds(200);
        // Offset the sprite so it actually looks like it's coming from the gun
        var offset = angle.ToVec().Normalized / 2;

        var message = new EffectSystemMessage
        {
            EffectSprite = component.MuzzleFlashSprite,
            Born = time,
            DeathTime = deathTime,
            AttachedEntityUid = entity,
            AttachedOffset = offset,
            //Rotated from east facing
            Rotation = (float) angle.Theta,
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 255), 1.0f),
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };

        _effects.CreateParticle(message);
    }
}
