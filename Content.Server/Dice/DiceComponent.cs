using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Dice
{
    [RegisterComponent]
    public class DiceComponent : Component, IActivate, IUse, ILand, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Dice";

        private int _sides = 20;

        [ViewVariables]
        [DataField("sound")]
        private readonly SoundSpecifier _sound = new SoundCollectionSpecifier("Dice");

        [ViewVariables]
        [DataField("step")]
        public int Step { get; } = 1;

        [ViewVariables]
        [DataField("sides")]
        public int Sides
        {
            get => _sides;
            set
            {
                _sides = value;
                CurrentSide = value;
            }
        }

        [ViewVariables]
        public int CurrentSide { get; private set; } = 20;

        public void Roll()
        {
            CurrentSide = _random.Next(1, (_sides/Step)+1) * Step;

            PlayDiceEffect();

            if (!Owner.TryGetComponent(out SpriteComponent? sprite))
                return;

            sprite.LayerSetState(0, $"d{_sides}{CurrentSide}");
        }

        public void PlayDiceEffect()
        {
            if(_sound.TryGetSound(out var sound))
                SoundSystem.Play(Filter.Pvs(Owner), sound, Owner, AudioParams.Default);
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
            message.AddMarkup(Loc.GetString("dice-component-on-examine-message-part-1",
                                            ("sidesAmount", _sides))
                              + "\n" +
                              Loc.GetString("dice-component-on-examine-message-part-2",
                                            ("currentSide", CurrentSide)));
        }
    }
}
