using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.MachineLinking;
using Content.Server.GameObjects.Components.MachineLinking.Signals;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class PoweredLightComponent : Component, IInteractHand, IInteractUsing, IMapInit, ISignalReceiver<bool>, ISignalReceiver<ToggleSignal>
    {
        [Dependency] private IGameTiming _gameTiming = default!;

        public override string Name => "PoweredLight";

        private static readonly TimeSpan _thunkDelay = TimeSpan.FromSeconds(2);
        private TimeSpan _lastThunk;

        [ViewVariables] private bool _on;

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

        // TODO CONSTRUCTION make this use a construction graph

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return InsertBulb(eventArgs.Using);
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IDamageableComponent damageableComponent))
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
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You burn your hand!"));
                damageableComponent.ChangeDamage(DamageType.Heat, 20, false, Owner);
                var audioSystem = EntitySystem.Get<AudioSystem>();
                audioSystem.PlayFromEntity("/Audio/Effects/lightburn.ogg", Owner);
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
                bulb.Owner.Transform.Coordinates = user.Transform.Coordinates;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref BulbType, "bulb", LightBulbType.Tube);
            serializer.DataField(ref _on, "on", true);
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
            var powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
            var sprite = Owner.GetComponent<SpriteComponent>();
            var light = Owner.GetComponent<PointLightComponent>();
            if (LightBulb == null) // No light bulb.
            {
                powerReceiver.Load = 0;
                sprite.LayerSetState(0, "empty");
                light.Enabled = false;
                return;
            }

            switch (LightBulb.State)
            {
                case LightBulbState.Normal:
                    if (powerReceiver.Powered && _on)
                    {
                        powerReceiver.Load = LightBulb.PowerUse;
                        sprite.LayerSetState(0, "on");
                        light.Enabled = true;
                        light.Color = LightBulb.Color;
                        var time = _gameTiming.CurTime;
                        if (time > _lastThunk + _thunkDelay)
                        {
                            _lastThunk = time;
                            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/light_tube_on.ogg", Owner, AudioParams.Default.WithVolume(-10f));
                        }
                    }
                    else
                    {
                        sprite.LayerSetState(0, "off");
                        light.Enabled = false;
                    }
                    break;
                case LightBulbState.Broken:
                    sprite.LayerSetState(0, "broken");
                    light.Enabled = false;
                    break;
                case LightBulbState.Burned:
                    sprite.LayerSetState(0, "burned");
                    light.Enabled = false;
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<PowerReceiverComponent>().OnPowerStateChanged += UpdateLight;

            _lightBulbContainer = ContainerManagerComponent.Ensure<ContainerSlot>("light_bulb", Owner);
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent receiver))
            {
                receiver.OnPowerStateChanged -= UpdateLight;
            }

            base.OnRemove();
        }

        void IMapInit.MapInit()
        {
            var prototype = BulbType switch
            {
                LightBulbType.Bulb => "LightBulb",
                LightBulbType.Tube => "LightTube",
                _ => throw new ArgumentOutOfRangeException()
            };

            var entity = Owner.EntityManager.SpawnEntity(prototype, Owner.Transform.Coordinates);
            _lightBulbContainer.Insert(entity);
        }

        public void TriggerSignal(bool signal)
        {
            _on = signal;
            UpdateLight();
        }

        public void TriggerSignal(ToggleSignal signal)
        {
            _on = !_on;
            UpdateLight();
        }
    }
}
