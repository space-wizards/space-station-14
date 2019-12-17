using System;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class PoweredLightComponent : Component, IAttackHand, IAttackBy
    {
        public override string Name => "PoweredLight";

        private static readonly TimeSpan _thunkDelay = TimeSpan.FromSeconds(2);

        private TimeSpan _lastThunk;

        private LightBulbType BulbType = LightBulbType.Tube;

        [ViewVariables] private ContainerSlot _lightBulbContainer;

        [ViewVariables]
        private LightBulbComponent LightBulb
        {
            get
            {
                if (_lightBulbContainer.ContainedEntity == null) return null;

                _lightBulbContainer.ContainedEntity.TryGetComponent(out LightBulbComponent bulb);

                return bulb;
            }
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            return InsertBulb(eventArgs.AttackWith);
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out DamageableComponent damageableComponent))
            {
                Eject();
                return false;
            }
            if(eventArgs.User.TryGetComponent(out HeatResistanceComponent heatResistanceComponent))
            {
                if(CanBurn(heatResistanceComponent.GetHeatResistance()))
                {
                    Burn();
                    return true;
                }
            }
            Eject();
            return true;

            bool CanBurn(int heatResistance)
            {
                return _lightState && heatResistance < LightBulb.BurningTemperature;
            }

            void Burn()
            {
                damageableComponent.TakeDamage(DamageType.Heat, 20);
            }

            void Eject()
            {
                EjectBulb(eventArgs.User);
                UpdateLight();
            }
        }

        /// <summary>
        ///     Inserts the bulb if possible.
        /// </summary>
        /// <returns>True if it could insert it, false if it couldn't.</returns>
        private bool InsertBulb(IEntity bulb)
        {
            if (LightBulb != null) return false;
            if (!bulb.TryGetComponent(out LightBulbComponent lightBulb)) return false;
            if (lightBulb.Type != BulbType) return false;

            var inserted = _lightBulbContainer.Insert(bulb);

            lightBulb.OnLightBulbStateChange += UpdateLight;
            lightBulb.OnLightColorChange += UpdateLight;

            UpdateLight();

            return inserted;
        }

        /// <summary>
        ///     Ejects the bulb to a mob's hand if possible.
        /// </summary>
        private void EjectBulb(IEntity user)
        {
            if (LightBulb == null) return;

            var bulb = LightBulb;

            bulb.OnLightBulbStateChange -= UpdateLight;
            bulb.OnLightColorChange -= UpdateLight;

            if (!_lightBulbContainer.Remove(bulb.Owner)) return;

            if (!user.TryGetComponent(out HandsComponent hands)
                || !hands.PutInHand(bulb.Owner.GetComponent<ItemComponent>()))
                bulb.Owner.Transform.GridPosition = user.Transform.GridPosition;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref BulbType, "bulb", LightBulbType.Tube);
        }

        /// <summary>
        ///     For attaching UpdateLight() to events.
        /// </summary>
        public void UpdateLight(object sender, EventArgs e)
        {
            UpdateLight();
        }

        private bool _lightState => Owner.GetComponent<PointLightComponent>().Enabled;

        /// <summary>
        ///     Updates the light's power drain, sprite and actual light state.
        /// </summary>
        public void UpdateLight()
        {
            var device = Owner.GetComponent<PowerDeviceComponent>();
            var sprite = Owner.GetComponent<SpriteComponent>();
            var light = Owner.GetComponent<PointLightComponent>();
            if (LightBulb == null) // No light bulb.
            {
                device.Load = 0;
                sprite.LayerSetState(0, "empty");
                light.Enabled = false;
                return;
            }

            switch (LightBulb.State)
            {
                case LightBulbState.Normal:
                    device.Load = LightBulb.PowerUse;
                    if (device.Powered)
                    {
                        sprite.LayerSetState(0, "on");
                        light.Enabled = true;
                        light.Color = LightBulb.Color;
                        var time = IoCManager.Resolve<IGameTiming>().CurTime;
                        if (time > _lastThunk + _thunkDelay)
                        {
                            _lastThunk = time;
                            Owner.GetComponent<SoundComponent>().Play("/Audio/machines/light_tube_on.ogg", AudioParams.Default.WithVolume(-10f));
                        }
                    }
                    else
                    {
                        sprite.LayerSetState(0, "off");
                        light.Enabled = false;
                    }
                    break;
                case LightBulbState.Broken:
                    device.Load = 0;
                    sprite.LayerSetState(0, "broken");
                    light.Enabled = false;
                    break;
                case LightBulbState.Burned:
                    device.Load = 0;
                    sprite.LayerSetState(0, "burned");
                    light.Enabled = false;
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var device = Owner.GetComponent<PowerDeviceComponent>();
            device.OnPowerStateChanged += UpdateLight;

            _lightBulbContainer = ContainerManagerComponent.Ensure<ContainerSlot>("light_bulb", Owner, out var existed);

            if (!existed) // Insert a light tube if there wasn't any.
            {
                switch (BulbType)
                {
                    case LightBulbType.Tube:
                        _lightBulbContainer.Insert(Owner.EntityManager.SpawnEntity("LightTube", Owner.Transform.GridPosition));
                        break;
                    case LightBulbType.Bulb:
                        _lightBulbContainer.Insert(Owner.EntityManager.SpawnEntity("LightBulb", Owner.Transform.GridPosition));
                        break;
                }
            }
        }
    }
}
