using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool used to weld metal together, light things on fire, or melt into constituent parts
    /// </summary>
    [RegisterComponent]
    class WelderComponent : ToolComponent, IUse, IExamine
    {
        SpriteComponent spriteComponent;

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Welder";

        /// <summary>
        /// Maximum fuel capacity the welder can hold
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float FuelCapacity
        {
            get => _fuelCapacity;
            set => _fuelCapacity = value;
        }
        private float _fuelCapacity = 50;

        /// <summary>
        /// Fuel the welder has to do tasks
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Fuel
        {
            get => _fuel;
            set => _fuel = value;
        }
        private float _fuel = 0;

        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const float DefaultFuelCost = 5;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public const float FuelLossRate = 0.2f;

        /// <summary>
        /// Status of welder, whether it is ignited
        /// </summary>
        [ViewVariables]
        public bool Activated { get; private set; } = false;

        //private string OnSprite { get; set; }
        //private string OffSprite { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = Owner.GetComponent<SpriteComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fuelCapacity, "Capacity", 50);
            serializer.DataField(ref _fuel, "Fuel", FuelCapacity);
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated)
            {
                return;
            }

            Fuel = Math.Max(Fuel - (FuelLossRate * frameTime), 0);

            if (Fuel == 0)
            {
                ToggleStatus();
            }
        }

        public bool TryUse(float value)
        {
            if (!Activated || !CanUse(value))
            {
                return false;
            }

            Fuel -= value;
            return true;
        }

        public bool CanUse(float value)
        {
            return Fuel > value;
        }

        public override bool CanUse()
        {
            return CanUse(DefaultFuelCost);
        }

        public bool CanActivate()
        {
            return Fuel > 0;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleStatus();
        }

        /// <summary>
        /// Deactivates welding tool if active, activates welding tool if possible
        /// </summary>
        /// <returns></returns>
        public bool ToggleStatus()
        {
            if (Activated)
            {
                Activated = false;
                // Layer 1 is the flame.
                spriteComponent.LayerSetVisible(1, false);
                PlaySoundCollection("welder_off", -5);
                return true;
            }
            else if (CanActivate())
            {
                Activated = true;
                spriteComponent.LayerSetVisible(1, true);
                PlaySoundCollection("welder_on", -5);
                return true;
            }
            else
            {
                return false;
            }
        }

        void IExamine.Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            if (Activated)
            {
                message.AddMarkup(loc.GetString("[color=orange]Lit[/color]\n"));
            }
            else
            {
                message.AddText(loc.GetString("Not lit\n"));
            }

            message.AddMarkup(loc.GetString("Fuel: [color={0}]{1}/{2}[/color].",
                Fuel < FuelCapacity / 4f ? "darkorange" : "orange", Math.Round(Fuel), FuelCapacity));
        }

        private void PlaySoundCollection(string name, float volume)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(name);
            var file = _robustRandom.Pick(soundCollection.PickFiles);
            _entitySystemManager.GetEntitySystem<AudioSystem>()
                .Play(file, AudioParams.Default.WithVolume(volume));
        }
    }
}
