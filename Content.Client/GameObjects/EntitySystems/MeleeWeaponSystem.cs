using Content.Client.GameObjects.Components.Mobs;
using Content.Client.GameObjects.Components.Weapons.Melee;
using Content.Shared.GameObjects.Components.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timers;
using static Content.Shared.GameObjects.EntitySystemMessages.MeleeWeaponSystemMessages;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<PlayMeleeWeaponAnimationMessage>(PlayWeaponArc);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var arcAnimationComponent in EntityManager.ComponentManager.EntityQuery<MeleeWeaponArcAnimationComponent>())
            {
                arcAnimationComponent.Update(frameTime);
            }
        }

        private void PlayWeaponArc(PlayMeleeWeaponAnimationMessage msg)
        {
            if (!_prototypeManager.TryIndex(msg.ArcPrototype, out MeleeWeaponAnimationPrototype weaponArc))
            {
                Logger.Error("Tried to play unknown weapon arc prototype '{0}'", msg.ArcPrototype);
                return;
            }

            var attacker = EntityManager.GetEntity(msg.Attacker);

            var lunge = attacker.EnsureComponent<MeleeLungeComponent>();
            lunge.SetData(msg.Angle);

            var entity = EntityManager.SpawnEntity(weaponArc.Prototype, attacker.Transform.GridPosition);
            entity.Transform.LocalRotation = msg.Angle;

            var weaponArcAnimation = entity.GetComponent<MeleeWeaponArcAnimationComponent>();
            weaponArcAnimation.SetData(weaponArc, msg.Angle, attacker);


            foreach (var uid in msg.Hits)
            {
                if (!EntityManager.TryGetEntity(uid, out var hitEntity))
                {
                    continue;
                }

                if (!hitEntity.TryGetComponent(out ISpriteComponent sprite))
                {
                    continue;
                }

                var originalColor = sprite.Color;
                var newColor = Color.Red * originalColor;
                sprite.Color = newColor;

                Timer.Spawn(100, () =>
                {
                    // Only reset back to the original color if something else didn't change the color in the mean time.
                    if (sprite.Color == newColor)
                    {
                        sprite.Color = originalColor;
                    }
                });
            }
        }
    }
}
