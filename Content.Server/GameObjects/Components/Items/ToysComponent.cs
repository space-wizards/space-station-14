using Content.Server.GameObjects.Components.Sound;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ToysComponent : Component, IActivate, IUse, ILand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Toys";

        [ViewVariables]
        [DataField("toySqueak")]
        public string _soundCollectionName = "ToySqueak";
        private EmitSoundOnLandComponent? _emitSoundOnLand = default;
        private EmitSoundOnUseComponent? _emitSoundOnUse = default;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent(out EmitSoundOnLandComponent emitSoundOnLand);
            Owner.EnsureComponent(out EmitSoundOnUseComponent emitSoundOnUse);
            _emitSoundOnLand = emitSoundOnLand;
            _emitSoundOnUse = emitSoundOnUse;
        }

        public void PlaySqueakEffect()
        {
            if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var file = SelectRandomSoundFromSoundCollection(_soundCollectionName);
                SoundSystem.Play(Filter.Pvs(Owner), file, Owner, AudioParams.Default);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            PlaySqueakEffect();
        }

        private string SelectRandomSoundFromSoundCollection(string soundCollectionName)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
            return _random.Pick(soundCollection.PickFiles);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if(_emitSoundOnUse != null)
            {
                _emitSoundOnUse._soundName = SelectRandomSoundFromSoundCollection(_soundCollectionName);
            }
            
            return false;
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            if(_emitSoundOnLand != null)
            {
                _emitSoundOnLand._soundName = SelectRandomSoundFromSoundCollection(_soundCollectionName);
            }          
        }
    }
}

