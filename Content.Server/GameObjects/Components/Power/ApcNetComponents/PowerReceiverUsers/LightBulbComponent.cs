#nullable enable
using System;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    public enum LightBulbState
    {
        Normal,
        Broken,
        Burned,
    }

    public enum LightBulbType
    {
        Bulb,
        Tube,
    }

    /// <summary>
    ///     Component that represents a light bulb. Can be broken, or burned, which turns them mostly useless.
    /// </summary>
    [RegisterComponent]
    public class LightBulbComponent : Component, ILand, IBreakAct
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        /// <summary>
        ///     Invoked whenever the state of the light bulb changes.
        /// </summary>
        public event EventHandler<EventArgs>? OnLightBulbStateChange;
        public event EventHandler<EventArgs?>? OnLightColorChange;

        [DataField("color")]
        private Color _color = Color.White;

        [ViewVariables(VVAccess.ReadWrite)] public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnLightColorChange?.Invoke(this, null);
                UpdateColor();
            }
        }

        public override string Name => "LightBulb";

        [DataField("bulb")]
        public LightBulbType Type = LightBulbType.Tube;

        [DataField("BurningTemperature")]
        private int _burningTemperature = 1400;
        public int BurningTemperature => _burningTemperature;

        [DataField("PowerUse")]
        private int _powerUse = 40;
        public int PowerUse => _powerUse;

        /// <summary>
        ///     The current state of the light bulb. Invokes the OnLightBulbStateChange event when set.
        ///     It also updates the bulb's sprite accordingly.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public LightBulbState State
        {
            get { return _state; }
            set
            {
                var sprite = Owner.GetComponent<SpriteComponent>();
                OnLightBulbStateChange?.Invoke(this, EventArgs.Empty);
                _state = value;
                switch (value)
                {
                    case LightBulbState.Normal:
                        sprite.LayerSetState(0, "normal");
                        break;
                    case LightBulbState.Broken:
                        sprite.LayerSetState(0, "broken");
                        break;
                    case LightBulbState.Burned:
                        sprite.LayerSetState(0, "burned");
                        break;
                }
            }
        }

        private LightBulbState _state = LightBulbState.Normal;

        public void UpdateColor()
        {
            if (!Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            sprite.Color = Color;
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdateColor();
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            PlayBreakSound();
            State = LightBulbState.Broken;
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            State = LightBulbState.Broken;
        }

        public void PlayBreakSound()
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>("GlassBreak");
            var file = _random.Pick(soundCollection.PickFiles);

            SoundSystem.Play(Filter.Pvs(Owner), file, Owner);
        }
    }
}
