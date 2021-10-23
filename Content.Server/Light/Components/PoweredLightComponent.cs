using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class PoweredLightComponent : Component, IInteractHand, IInteractUsing, IMapInit
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "PoweredLight";

        private static readonly TimeSpan _thunkDelay = TimeSpan.FromSeconds(2);

        [ComponentDependency]
        private readonly AppearanceComponent? _appearance;

        private TimeSpan _lastThunk;
        public TimeSpan? LastGhostBlink;

        [DataField("burnHandSound")]
        private SoundSpecifier _burnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        [DataField("turnOnSound")]
        private SoundSpecifier _turnOnSound = new SoundPathSpecifier("/Audio/Machines/light_tube_on.ogg");

        [DataField("hasLampOnSpawn")]
        private bool _hasLampOnSpawn = true;

        [ViewVariables]
        [DataField("on")]
        private bool _on = true;

        [ViewVariables]
        private bool _currentLit;

        [ViewVariables]
        public bool IsBlinking;

        [ViewVariables]
        [DataField("ignoreGhostsBoo")]
        public bool IgnoreGhostsBoo;

        [ViewVariables]
        [DataField("ghostBlinkingTime")]
        public TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);

        [ViewVariables]
        [DataField("ghostBlinkingCooldown")]
        public TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);


        [DataField("bulb")] private LightBulbType _bulbType = LightBulbType.Tube;
        public LightBulbType BulbType => _bulbType;

        [ViewVariables] private ContainerSlot _lightBulbContainer = default!;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        protected override void Initialize()
        {
            base.Initialize();
            _lightBulbContainer = Owner.EnsureContainer<ContainerSlot>("light_bulb");
        }

        [ViewVariables]
        public LightBulbComponent? LightBulb
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
            if (!eventArgs.User.HasComponent<DamageableComponent>())
            {
                Eject();
                return false;
            }
            if (eventArgs.User.TryGetComponent(out HeatResistanceComponent? heatResistanceComponent))
            {
                if (CanBurn(heatResistanceComponent.GetHeatResistance()))
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

                return _currentLit && heatResistance < LightBulb.BurningTemperature;
            }

            void Burn()
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("powered-light-component-burn-hand"));
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(eventArgs.User.Uid, Damage);
                SoundSystem.Play(Filter.Pvs(Owner), _burnHandSound.GetSound(), Owner);
            }

            void Eject()
            {
                EjectBulb(eventArgs.User);
                UpdateLight();
            }
        }

        /// <summary>
        /// Try to replace current bulb with a new one
        /// </summary>
        public bool ReplaceBulb(IEntity bulb)
        {
            EjectBulb();
            return InsertBulb(bulb);
        }

        /// <summary>
        ///     Inserts the bulb if possible.
        /// </summary>
        /// <returns>True if it could insert it, false if it couldn't.</returns>
        private bool InsertBulb(IEntity bulb)
        {
            if (LightBulb != null) return false;
            if (!bulb.TryGetComponent(out LightBulbComponent? lightBulb)) return false;
            if (lightBulb.Type != _bulbType) return false;

            var inserted = _lightBulbContainer.Insert(bulb);

            lightBulb.OnLightBulbStateChange += UpdateLight;
            lightBulb.OnLightColorChange += UpdateLight;

            UpdateLight();

            return inserted;
        }

        /// <summary>
        ///     Ejects the bulb to a mob's hand if possible.
        /// </summary>
        private void EjectBulb(IEntity? user = null)
        {
            if (LightBulb == null) return;

            var bulb = LightBulb;

            bulb.OnLightBulbStateChange -= UpdateLight;
            bulb.OnLightColorChange -= UpdateLight;

            if (!_lightBulbContainer.Remove(bulb.Owner)) return;

            if (user != null)
            {
                if (!user.TryGetComponent(out HandsComponent? hands)
                    || !hands.PutInHand(bulb.Owner.GetComponent<ItemComponent>()))
                    bulb.Owner.Transform.Coordinates = user.Transform.Coordinates;
            }
            else
            {
                bulb.Owner.Transform.Coordinates = Owner.Transform.Coordinates;
            }

        }

        /// <summary>
        ///     For attaching UpdateLight() to events.
        /// </summary>
        public void UpdateLight(object? sender, EventArgs? e)
        {
            UpdateLight();
        }

        /// <summary>
        ///     Updates the light's power drain, sprite and actual light state.
        /// </summary>
        public void UpdateLight()
        {
            var powerReceiver = Owner.GetComponent<ApcPowerReceiverComponent>();
            powerReceiver.Load = (LightBulb != null && _on && LightBulb.State == LightBulbState.Normal) ? LightBulb.PowerUse : 0;

            if (LightBulb == null) // No light bulb.
            {
                SetLight(false);
                _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Empty);
                return;
            }

            switch (LightBulb.State)
            {
                case LightBulbState.Normal:
                    if (powerReceiver.Powered && _on)
                    {
                        SetLight(true, LightBulb.Color);
                        _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.On);
                        var time = _gameTiming.CurTime;
                        if (time > _lastThunk + _thunkDelay)
                        {
                            _lastThunk = time;
                            SoundSystem.Play(Filter.Pvs(Owner), _turnOnSound.GetSound(), Owner, AudioParams.Default.WithVolume(-10f));
                        }
                    }
                    else
                    {
                        SetLight(false);
                        _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Off);
                    }
                    break;
                case LightBulbState.Broken:
                    SetLight(false);
                    _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Broken);
                    break;
                case LightBulbState.Burned:
                    SetLight(false);
                    _appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Burned);
                    break;
            }
        }

        private void SetLight(bool value, Color? color = null)
        {
            _currentLit = value;
            EntitySystem.Get<SharedAmbientSoundSystem>().SetAmbience(Owner.Uid, value);

            if (!Owner.TryGetComponent(out PointLightComponent? pointLight)) return;
            pointLight.Enabled = value;

            if (color != null)
                pointLight.Color = color.Value;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage:
                    UpdateLight();
                    break;
            }
        }

        public void TryDestroyBulb()
        {
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
                var prototype = _bulbType switch
                {
                    LightBulbType.Bulb => "LightBulb",
                    LightBulbType.Tube => "LightTube",
                    _ => throw new ArgumentOutOfRangeException()
                };

                var entity = Owner.EntityManager.SpawnEntity(prototype, Owner.Transform.Coordinates);
                _lightBulbContainer.Insert(entity);
            }

            // need this to update visualizers
            UpdateLight();
        }

        public void ToggleLight()
        {
            _on = !_on;
            UpdateLight();
        }

        public void SetState(bool state)
        {
            _on = state;
            UpdateLight();
        }
    }
}
