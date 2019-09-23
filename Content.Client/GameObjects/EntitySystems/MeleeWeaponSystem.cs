using Content.Client.GameObjects.Components.Weapons.Melee;
using Content.Shared.GameObjects.Components.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.EntitySystemMessages.MeleeWeaponSystemMessages;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class MeleeWeaponSystem : EntitySystem
    {

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(MeleeWeaponArcAnimationComponent));
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<PlayWeaponArcMessage>();
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case PlayWeaponArcMessage playMsg:
                    PlayWeaponArc(playMsg);
                    break;
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<MeleeWeaponArcAnimationComponent>().Update(frameTime);
            }
        }

        private void PlayWeaponArc(PlayWeaponArcMessage msg)
        {
            if (!_prototypeManager.TryIndex(msg.ArcPrototype, out WeaponArcPrototype weaponArc))
            {
                Logger.Error("Tried to play unknown weapon arc prototype '{0}'", msg.ArcPrototype);
                return;
            }

            var entity = EntityManager.SpawnEntityAt("WeaponArc", msg.Position);
            entity.Transform.LocalRotation = msg.Angle;

            var weaponArcAnimation = entity.GetComponent<MeleeWeaponArcAnimationComponent>();
            weaponArcAnimation.SetData(weaponArc, msg.Angle);
        }
    }
}
