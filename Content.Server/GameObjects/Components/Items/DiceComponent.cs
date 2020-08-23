using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
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
    public class DiceComponent : Component, IActivate, IUse, ILand, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public override string Name => "Dice";

        private int _step = 1;
        private int _sides = 20;
        private int _currentSide = 20;
        [ViewVariables]
        public string _soundCollectionName = "dice";
        [ViewVariables]
        public int Step => _step;
        [ViewVariables]
        public int Sides => _sides;
        [ViewVariables]
        public int CurrentSide => _currentSide;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _step, "step", 1);
            serializer.DataField(ref _sides, "sides", 20);
            serializer.DataField(ref _soundCollectionName, "diceSoundCollection", "dice");
            _currentSide = _sides;
        }

        public void Roll()
        {
            _currentSide = _random.Next(1, (_sides/_step)+1) * _step;
            if (!Owner.TryGetComponent(out SpriteComponent sprite)) return;
            sprite.LayerSetState(0, $"d{_sides}{_currentSide}");
            PlayDiceEffect();
        }

        public void PlayDiceEffect()
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
            Roll();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Roll();
            return false;
        }

        public void Land(LandEventArgs eventArgs)
        {
            Roll();
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            //No details check, since the sprite updates to show the side.
            message.AddMarkup(Loc.GetString(
                "A dice with [color=lightgray]{0}[/color] sides.\n" + "It has landed on a [color=white]{1}[/color].",
                _sides, _currentSide));
        }
    }
}
