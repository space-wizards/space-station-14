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
        if (component.MuzzleFlashSprite == null)
        {
            return;
        }

        var time = _gameTiming.CurTime;
        var deathTime = time + TimeSpan.FromMilliseconds(200);
        // Offset the sprite so it actually looks like it's coming from the gun
        var offset = new Vector2(0.0f, -0.5f);

        var message = new EffectSystemMessage
        {
            EffectSprite = component.MuzzleFlashSprite.ToString(),
            Born = time,
            DeathTime = deathTime,
            AttachedEntityUid = entity,
            AttachedOffset = offset,
            //Rotated from east facing
            Rotation = -MathF.PI / 2f,
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 255), 1.0f),
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };

        /* TODO: Fix rotation when shooting sideways. This was the closest I got but still had issues.
         * var time = _gameTiming.CurTime;
        var deathTime = time + TimeSpan.FromMilliseconds(200);
        var entityRotation = EntityManager.GetComponent<TransformComponent>(entity).WorldRotation;
        var localAngle = entityRotation - (angle + MathF.PI / 2f);
        // Offset the sprite so it actually looks like it's coming from the gun
        var offset = localAngle.RotateVec(new Vector2(0.0f, -0.5f));

        var message = new EffectSystemMessage
        {
            EffectSprite = component.MuzzleFlashSprite.ToString(),
            Born = time,
            DeathTime = deathTime,
            AttachedEntityUid = entity,
            AttachedOffset = offset,
            //Rotated from east facing
            Rotation = (float) (localAngle - MathF.PI / 2),
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 255), 1.0f),
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };
         */

        _effects.CreateParticle(message);
    }
}
