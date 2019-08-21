using System;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
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
                Owner.GetComponent<SoundComponent>().Play(file, AudioParams.Default);
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

        public void Examine(FormattedMessage message)
        {
            message.AddText("A dice with ");
            message.PushColor(new Color(1F, 0.75F, 0.75F));
            message.AddText(_sides.ToString());
            message.Pop();
            message.AddText(" sides.\nIt has landed on a ");
            message.PushColor(new Color(1F, 1F, 1F));
            message.AddText(_currentSide.ToString());
            message.Pop();
            message.AddText(".");
        }
    }
}
