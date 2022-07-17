using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Dice
{
    [UsedImplicitly]
    public sealed class DiceSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiceComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DiceComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<DiceComponent, LandEvent>(OnLand);
            SubscribeLocalEvent<DiceComponent, ExaminedEvent>(OnExamined);
        }

        private void OnComponentInit(EntityUid uid, DiceComponent component, ComponentInit args)
        {
            if (component.CurrentSide > component.Sides)
                component.CurrentSide = component.Sides;
        }

        private void OnUseInHand(EntityUid uid, DiceComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;

            args.Handled = true;
            Roll(uid, component);
        }

        private void OnLand(EntityUid uid, DiceComponent component, LandEvent args)
        {
            Roll(uid, component);
        }

        private void OnExamined(EntityUid uid, DiceComponent dice, ExaminedEvent args)
        {
            //No details check, since the sprite updates to show the side.
            args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-1", ("sidesAmount", dice.Sides)));
            args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-2", ("currentSide", dice.CurrentSide)));
        }

        public void SetCurrentSide(EntityUid uid, int side, DiceComponent? die = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref die, ref sprite))
                return;

            side = Math.Min(Math.Max(side, 1), die.Sides);
            side += side % die.Step;

            die.CurrentSide = side;

            // TODO DICE: Use a visualizer instead.
            sprite.LayerSetState(0, $"d{die.Sides}{die.CurrentSide}");
        }

        public void Roll(EntityUid uid, DiceComponent? die = null)
        {
            if (!Resolve(uid, ref die))
                return;

            var roll = _random.Next(1, die.Sides/die.Step+1) * die.Step;
            SetCurrentSide(uid, roll, die);

            die.Owner.PopupMessageEveryone(Loc.GetString("dice-component-on-roll-land", ("die", die.Owner), ("currentSide", die.CurrentSide)));
            SoundSystem.Play(die.Sound.GetSound(), Filter.Pvs(die.Owner), die.Owner, AudioHelpers.WithVariation(0.05f));
        }
    }
}
