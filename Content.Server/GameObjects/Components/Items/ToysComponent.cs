using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Utility;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class ToysComponent : Component, IActivate, IUse, ILand
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public override string Name => "Toys";

        [ViewVariables]
        public string _soundCollectionName = "ToySqueak";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundCollectionName, "toySqueak", "ToySqueak");
        }

        public void Squeak()
        {
            PlaySqueakEffect();
        }

        public void PlaySqueakEffect()
        {
            if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
                var file = _random.Pick(soundCollection.PickFiles);
                EntitySystem.Get<AudioSystem>().PlayFromEntity(file, Owner, AudioParams.Default);
            }
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            Squeak();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Squeak();
            return false;
        }
        public void Land(LandEventArgs eventArgs)
        {
            Squeak();
        }
    }
}

