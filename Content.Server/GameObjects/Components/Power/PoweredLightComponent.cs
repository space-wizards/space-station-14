using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    public class PoweredLightComponent : Component, IAttackHand, IAttackby
    {
        public override string Name => "PoweredLight";

        private static readonly TimeSpan _thunkDelay = TimeSpan.FromSeconds(2);

        private TimeSpan _lastThunk;

        private LightBulbType BulbType = LightBulbType.Tube;

        private float Load = 40;

        [ViewVariables] private ContainerSlot LightBulbContainer;

        [ViewVariables]
        private LightBulbComponent LightBulb
        {
            get
            {
                if (LightBulbContainer.ContainedEntity == null) return null;

                LightBulbContainer.ContainedEntity.TryGetComponent(out LightBulbComponent bulb);

                return bulb;
            }
        }

        bool IAttackby.Attackby(IEntity user, IEntity attackwith)
        {
            if (!attackwith.HasComponent<LightBulbComponent>()) return false;

            if (LightBulb != null) return false;

            user.GetComponent<IHandsComponent>().Drop(attackwith, LightBulbContainer);

            var inserted = LightBulbContainer.Insert(attackwith);

            UpdateLight();

            return inserted;
        }

        bool IAttackHand.Attackhand(IEntity user)
        {
            if (user.GetComponent<InventoryComponent>().GetSlotItem(EquipmentSlotDefines.Slots.GLOVES) != null)
            {
                EjectBulb(user);
                UpdateLight();
                return true;
            }
            else
            {
                if (!user.TryGetComponent(out DamageableComponent damageableComponent)) return false;
                damageableComponent.TakeDamage(DamageType.Heat, 20);
            }

            UpdateLight();

            return false;
        }

        private void EjectBulb(IEntity user)
        {
            if (LightBulb == null) return;

            var bulb = LightBulb;

            if (!LightBulbContainer.Remove(bulb.Owner)) return;

            if (!user.TryGetComponent(out HandsComponent hands)
                || !hands.PutInHand(bulb.Owner.GetComponent<ItemComponent>()))
                bulb.Owner.Transform.GridPosition = user.Transform.GridPosition;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Load, "load", 40);
            serializer.DataField(ref BulbType, "bulb", LightBulbType.Tube);
        }

        public void UpdateLight(object sender, EventArgs e)
        {
            UpdateLight();
        }

        public void UpdateLight()
        {
            var device = Owner.GetComponent<PowerDeviceComponent>();
            var sprite = Owner.GetComponent<SpriteComponent>();
            var light = Owner.GetComponent<PointLightComponent>();
            if (LightBulb == null)
            {
                device.Load = 0;
                sprite.LayerSetState(0, "empty");
                light.State = LightState.Off;
                return;
            }

            switch (LightBulb.State)
            {
                case LightBulbState.Normal:
                    if (device.Powered)
                    {
                        device.Load = Load;
                        sprite.LayerSetState(0, "on");
                        light.State = LightState.On;
                        var time = IoCManager.Resolve<IGameTiming>().CurTime;
                        if (time > _lastThunk + _thunkDelay)
                        {
                            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>()
                                .Play("/Audio/machines/light_tube_on.ogg", Owner, AudioParams.Default.WithVolume(-10f));
                        }
                    }
                    else
                    {
                        device.Load = 0;
                        sprite.LayerSetState(0, "off");
                        light.State = LightState.Off;
                    }

                    break;
                case LightBulbState.Broken:
                    device.Load = 0;
                    sprite.LayerSetState(0, "broken");
                    light.State = LightState.Off;
                    break;
                case LightBulbState.Burned:
                    device.Load = 0;
                    sprite.LayerSetState(0, "burned");
                    light.State = LightState.Off;
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var device = Owner.GetComponent<PowerDeviceComponent>();
            device.OnPowerStateChanged += UpdateLight;

            LightBulbContainer = ContainerManagerComponent.Ensure<ContainerSlot>("light_bulb", Owner, out var existed);

            if (!existed)
            {
                LightBulbContainer.Insert(Owner.EntityManager.SpawnEntity("LightTube"));
            }
        }
    }
}
