#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.MachineLinking;
using Content.Server.GameObjects.Components.MachineLinking.Signals;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class PoweredLightComponent : Component, IInteractHand, IInteractUsing, IMapInit, ISignalReceiver<bool>, ISignalReceiver<ToggleSignal>, IGhostBooAffected
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "PoweredLight";

        private static readonly TimeSpan _thunkDelay = TimeSpan.FromSeconds(2);
        // time to blink light when ghost made boo nearby
        private static readonly TimeSpan ghostBlinkingTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ghostBlinkingCooldown = TimeSpan.FromSeconds(60);

        [ComponentDependency]
        private readonly AppearanceComponent? _appearance;

        private TimeSpan _lastThunk;
        private TimeSpan? _lastGhostBlink;
        private bool _hasLampOnSpawn;

        [ViewVariables] private bool _on;
        [ViewVariables] private bool _isBlinking;
        [ViewVariables] private bool _ignoreGhostsBoo;

        private LightBulbType BulbType = LightBulbType.Tube;
        [ViewVariables] private ContainerSlot _lightBulbContainer = default!;

        [ViewVariables]
        private LightBulbComponent? LightBulb
        {
            get
            {
                if (_lightBulbContainer.ContainedEntity == null) return null;

                _lightBulbContainer.ContainedEntity.TryGetComponent(out LightBulbComponent? bulb);

                return bulb;
            }
        }

        // TODO CONSTRUCTION make this use a construction graph

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return InsertBulb(eventArgs.Using);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IDamageableComponent? damageableComponent))
            {
                Eject();
                return false;
            }
            if(eventArgs.User.TryGetComponent(out HeatResistanceComponent? heatResistanceComponent))
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
                if (LightBulb == null)
                    return false;

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
            if (!bulb.TryGetComponent(out LightBulbComponent? lightBulb)) return false;
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

            if (!user.TryGetComponent(out HandsComponent? hands)
                || !hands.PutInHand(bulb.Owner.GetComponent<ItemComponent>()))
                bulb.Owner.Transform.Coordinates = user.Transform.Coordinates;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref BulbType, "bulb", LightBulbType.Tube);
            serializer.DataField(ref _on, "on", true);
            serializer.DataField(ref _hasLampOnSpawn, "hasLampOnSpawn", true);
            serializer.DataField(ref _ignoreGhostsBoo, "ignoreGhostsBoo", false);
        }

        /// <summary>
        ///     For attaching UpdateLight() to events.
        /// </summary>
        public void UpdateLight(object? sender, EventArgs? e)
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

            if (LightBulb == null) // No light bulb.
            {
                powerReceiver.Load = 0;
                _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Empty);
                return;
            }

            switch (LightBulb.State)
            {
                case LightBulbState.Normal:
                    if (powerReceiver.Powered && _on)
                    {
                        powerReceiver.Load = LightBulb.PowerUse;
                        _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.On);
                        _appearance?.SetData(PoweredLightVisuals.BulbColor, LightBulb.Color);
                        var time = _gameTiming.CurTime;
                        if (time > _lastThunk + _thunkDelay)
                        {
                            _lastThunk = time;
                            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/light_tube_on.ogg", Owner, AudioParams.Default.WithVolume(-10f));
                        }
                    }
                    else
                    {
                        _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Off);
                    }
                    break;
                case LightBulbState.Broken:
                    _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Broken);
                    break;
                case LightBulbState.Burned:
                    _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Burned);
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _lightBulbContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "light_bulb");
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage:
                    UpdateLight();
                    break;
                case DamageChangedMessage msg:
                    TryDestroyBulb(msg);
                    break;
            }
        }

        private void TryDestroyBulb(DamageChangedMessage msg)
        {
            if (!msg.TookDamage)
                return;

            if (LightBulb == null || LightBulb.State == LightBulbState.Broken)
                return;

            LightBulb.State = LightBulbState.Broken;
            LightBulb.PlayBreakSound();
            UpdateLight();
        }

        void IMapInit.MapInit()
        {
            if (_hasLampOnSpawn)
            {
                var prototype = BulbType switch
                {
                    LightBulbType.Bulb => "LightBulb",
                    LightBulbType.Tube => "LightTube",
                    _ => throw new ArgumentOutOfRangeException()
                };

                var entity = Owner.EntityManager.SpawnEntity(prototype, Owner.Transform.Coordinates);
                _lightBulbContainer.Insert(entity);
                UpdateLight();
            }
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

        public void ToggleBlinkingLight(bool isNowBlinking)
        {
            if (_isBlinking == isNowBlinking)
                return;

            _isBlinking = isNowBlinking;
            _appearance?.SetData(PoweredLightVisuals.Blinking, _isBlinking);
        }

        public bool AffectedByGhostBoo(InstantActionEventArgs args)
        {
            if (_ignoreGhostsBoo)
                return false;

            // check cooldown first to prevent abuse
            var time = _gameTiming.CurTime;
            if (_lastGhostBlink != null)
            {
                if (time <= _lastGhostBlink + ghostBlinkingCooldown)
                    return false;
            }
            _lastGhostBlink = time;

            ToggleBlinkingLight(true);
            Owner.SpawnTimer(ghostBlinkingTime, () => {
                ToggleBlinkingLight(false);
            });

            return true;
        }
    }
}
