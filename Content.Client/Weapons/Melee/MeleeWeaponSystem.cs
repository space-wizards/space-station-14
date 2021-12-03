using System;
using Content.Client.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using static Content.Shared.Weapons.Melee.MeleeWeaponSystemMessages;

namespace Content.Client.Weapons.Melee
{
    [UsedImplicitly]
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly EffectSystem _effectSystem = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<PlayMeleeWeaponAnimationMessage>(PlayWeaponArc);
            SubscribeNetworkEvent<PlayLungeAnimationMessage>(PlayLunge);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var arcAnimationComponent in EntityManager.EntityQuery<MeleeWeaponArcAnimationComponent>(true))
            {
                arcAnimationComponent.Update(frameTime);
            }
        }

        private void PlayWeaponArc(PlayMeleeWeaponAnimationMessage msg)
        {
            if (!_prototypeManager.TryIndex(msg.ArcPrototype, out MeleeWeaponAnimationPrototype? weaponArc))
            {
                Logger.Error("Tried to play unknown weapon arc prototype '{0}'", msg.ArcPrototype);
                return;
            }

            if (!EntityManager.TryGetEntity(msg.Attacker, out var attacker))
            {
                // FIXME: This should never happen.
                Logger.Error($"Tried to play a weapon arc {msg.ArcPrototype}, but the attacker does not exist. attacker={msg.Attacker}, source={msg.Source}");
                return;
            }

            if (!((!IoCManager.Resolve<IEntityManager>().EntityExists(attacker.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(attacker.Uid).EntityLifeStage) >= EntityLifeStage.Deleted))
            {
                var lunge = attacker.EnsureComponent<MeleeLungeComponent>();
                lunge.SetData(msg.Angle);

                var entity = EntityManager.SpawnEntity(weaponArc.Prototype, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(attacker.Uid).Coordinates);
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity.Uid).LocalRotation = msg.Angle;

                var weaponArcAnimation = IoCManager.Resolve<IEntityManager>().GetComponent<MeleeWeaponArcAnimationComponent>(entity.Uid);
                weaponArcAnimation.SetData(weaponArc, msg.Angle, attacker, msg.ArcFollowAttacker);

                // Due to ISpriteComponent limitations, weapons that don't use an RSI won't have this effect.
                if (EntityManager.TryGetEntity(msg.Source, out var source) &&
                    msg.TextureEffect &&
                    IoCManager.Resolve<IEntityManager>().TryGetComponent(source.Uid, out ISpriteComponent? sourceSprite) &&
                    sourceSprite.BaseRSI?.Path != null)
                {
                    var curTime = _gameTiming.CurTime;
                    var effect = new EffectSystemMessage
                    {
                        EffectSprite = sourceSprite.BaseRSI.Path.ToString(),
                        RsiState = sourceSprite.LayerGetState(0).Name,
                        Coordinates = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(attacker.Uid).Coordinates,
                        Color = Vector4.Multiply(new Vector4(255, 255, 255, 125), 1.0f),
                        ColorDelta = Vector4.Multiply(new Vector4(0, 0, 0, -10), 1.0f),
                        Velocity = msg.Angle.ToWorldVec(),
                        Acceleration = msg.Angle.ToWorldVec() * 5f,
                        Born = curTime,
                        DeathTime = curTime.Add(TimeSpan.FromMilliseconds(300f)),
                    };

                    _effectSystem.CreateEffect(effect);
                }
            }

            foreach (var uid in msg.Hits)
            {
                if (!EntityManager.TryGetEntity(uid, out var hitEntity) || (!IoCManager.Resolve<IEntityManager>().EntityExists(hitEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(hitEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                {
                    continue;
                }

                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(hitEntity.Uid, out ISpriteComponent? sprite))
                {
                    continue;
                }

                var originalColor = sprite.Color;
                var newColor = Color.Red * originalColor;
                sprite.Color = newColor;

                hitEntity.SpawnTimer(100, () =>
                {
                    // Only reset back to the original color if something else didn't change the color in the mean time.
                    if (sprite.Color == newColor)
                    {
                        sprite.Color = originalColor;
                    }
                });
            }
        }

        private void PlayLunge(PlayLungeAnimationMessage msg)
        {
            if (EntityManager.TryGetEntity(msg.Source, out var entity))
            {
                entity.EnsureComponent<MeleeLungeComponent>().SetData(msg.Angle);
            }
            else
            {
                // FIXME: This should never happen.
                Logger.Error($"Tried to play a lunge animation, but the entity \"{msg.Source}\" does not exist.");
            }
        }
    }
}
