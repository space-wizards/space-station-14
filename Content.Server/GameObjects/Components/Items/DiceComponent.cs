using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class DiceComponent : Component, IActivate, IUse, ILand, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Dice";

        [DataField("step")]
        private int _step = 1;
        private int _sides = 20;
        private int _currentSide = 20;
        [ViewVariables]
        [DataField("diceSoundCollection")]
        public string _soundCollectionName = "dice";
        [ViewVariables]
        public int Step => _step;
        [ViewVariables]
        [DataField("sides")]
        public int Sides
        {
            get => _sides;
            set
            {
                _sides = value;
                _currentSide = value;
            }
        }

        [ViewVariables]
        public int CurrentSide => _currentSide;

        public void Roll()
        {
            _currentSide = _random.Next(1, (_sides/_step)+1) * _step;
            if (!Owner.TryGetComponent(out SpriteComponent? sprite)) return;
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

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            Roll();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Roll();
            return false;
        }

        void ILand.Land(LandEventArgs eventArgs)
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
