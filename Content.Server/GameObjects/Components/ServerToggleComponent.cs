using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
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

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    class ToggleComponent : IUse, IExamine
    {
        SpriteComponent spriteComponent;

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Toggle";
        public override uint? NetID => ContentNetIDs.TOGGLE;

        /// <summary>
        /// Maximum ammount the item can hold
        /// </summary>
        private int _ammountCapacity;
        /// <summary>
        /// "Ammount" the item has at the momment to do tasks
        /// </summary>
        private float _ammount;
        /// <summary>
        /// Cost of using the item for an action
        /// </summary>
        private float _ammountCost;
        /// <summary>
        /// How much ammount is lost for having the item on
        /// </summary>
        private float _ammountLossRate;
        /// <summary>
        /// Checks if the item is activated or not
        /// </summary>
        private bool _activated;
        /// <summary>
        /// Sound that plays when toggled
        /// </summary>
        private string _soundOn;
        private string _soundOff;
        /// <summary>
        /// IExamine menu
        /// </sumarry>
        private string _ammountName;
        private string _ammountColor1;
        private string _ammountColor2; 

       [ViewVariables(VVAccess.ReadWrite)]
        public int AmmountCapacity
        {
            get => _ammountCapacity;
            set => _ammountCapacity = value;
        } 

       [ViewVariables(VVAccess.ReadWrite)]
        public float Ammount
        {
            get => _ammount;
            set => _ammount = value;
        } 

       [ViewVariables(VVAccess.ReadWrite)]
        public float AmmountCost
        {
            get => _ammountCost;
            set => _ammountCost = value;
        } 

       [ViewVariables(VVAccess.ReadWrite)]
        public float AmmountLossRate
        {
            get => _ammountLossRate;
            set => _ammountLossRate = value;
        } 

       [ViewVariables(VVAccess.ReadWrite)]
        public bool Activated
        {
            get => _activated;
            set => _activated = value;
        } 

       [ViewVariables(VVAccess.ReadWrite)]
        public string SoundOn
        {
            get => _soundOn;
            set => _soundOn = value;
        } 

       [ViewVariables(VVAccess.ReadWrite)]
        public string SoundOff
        {
            get => _soundOff;
            set => _soundOff = value;
        }
         
       [ViewVariables(VVAccess.ReadWrite)]
        public string AmmountName
        {
            get => _ammountName;
            set => _ammountName = value;
        }
        
       [ViewVariables(VVAccess.ReadWrite)]
        public string AmmountColor1
        {
            get => _ammountColor1;
            set => _ammountColor1 = value;
        }
         
       [ViewVariables(VVAccess.ReadWrite)]
        public string AmmountColor2
        {
            get => _ammountColor2;
            set => _ammountColor2 = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {   
            base.ExposeData(serializer);
        
            serializer.DataField(ref _ammountCapacity, "Capacity", 20);
            serializer.DataField(ref _ammount, "Ammount", AmmountCapacity);
            serializer.DataField(ref _ammountCost, "Cost", 1);
            serializer.DataField(ref _ammountLossRate, "LossRate", 0.2f);
            serializer.DataField(ref _activated, "Activated", false);
            serializer.DataField(ref _soundOn, "SoundOn", "/Audio/Items/machines/machine_switch.ogg");
            serializer.DataField(ref _soundOff, "SoundOff", "/Audio/Items/machines/machine_switch.ogg");
            serializer.DataField(ref _ammountName, "Name", "Ammount");
            serializer.DataField(ref _ammountColor1, "Color1", "blue");
            serializer.DataField(ref _ammountColor2, "Color2", "red");
        }

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = Owner.GetComponent<SpriteComponent>();
        }

        public void OnUpdate(float frameTime)
        {
            if (!Activated)
            {
                return;
            }

            Ammount = Math.Max(Ammount - (AmmountLossRate * frameTime), 0);

            if (Ammount == 0)
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

            Ammount -= value;
            return true;
        }

        public bool CanUse(float value)
        {
            return Ammount > value;
        }

        public override bool CanUse()
        {
            return CanUse(AmmountCost);
        }

        public bool CanActivate()
        {
            return Ammount > 0;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleStatus();
        }

        /// <summary>
        /// Deactivates the item if active, activates the item if possible
        /// </summary>
        /// <returns></returns>
        public bool ToggleStatus()
        {
            if (Activated)
            {
                Activated = false;
                // Layer 1 is the flame.
                spriteComponent.LayerSetVisible(1, false);
                PlaySoundCollection(_soundOn, -1);
                return true;
            }
            else if (CanActivate())
            {
                Activated = true;
                spriteComponent.LayerSetVisible(1, true);
                PlaySoundCollection(_soundOff, -1);
                return true;
            }
            else
            {
                return false;
            }
        }

        void IExamine.Examine(FormattedMessage message)
        {
            message.AddMarkup(loc.GetString(AmmountName+": [color={0}]{1}/{2}[/color].",
                Ammount < AmmountCapacity / 2f ? AmmountColor1 : AmmountColor2, Math.Round(Ammount), AmmountCapacity));
        }

       /// private void PlaySoundCollection(string name, float volume)
       /// {
       ///     var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(name);
       ///     var file = _robustRandom.Pick(soundCollection.PickFiles);
       ///     _entitySystemManager.GetEntitySystem<AudioSystem>()
       ///         .Play(file, AudioParams.Default.WithVolume(volume));
       /// }

        public override ComponentState GetComponentState()
        {
            return new ToggleComponentState(AmmountCapacity,Ammount, AmmountCost, AmmountLossRate, Activated, 
            SoundOn, SoundOff, AmmountName, AmmountColor1, AmmountColor2);
        }
    }
}
