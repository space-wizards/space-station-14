using System;
using Content.Server.Notification;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Dice
{
    [UsedImplicitly]
    public class DiceSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiceComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DiceComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<DiceComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DiceComponent, LandEvent>(OnLand);
            SubscribeLocalEvent<DiceComponent, ExaminedEvent>(OnExamined);
        }

        private void OnComponentInit(EntityUid uid, DiceComponent component, ComponentInit args)
        {
            if (component.CurrentSide > component.Sides)
                component.CurrentSide = component.Sides;
        }

        private void OnActivate(EntityUid uid, DiceComponent component, ActivateInWorldEvent args)
        {
            Roll(uid, component);
        }

        private void OnUse(EntityUid uid, DiceComponent component, UseInHandEvent args)
        {
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
            SoundSystem.Play(Filter.Pvs(die.Owner), die.Sound.GetSound(), die.Owner, AudioHelpers.WithVariation(0.05f));
        }
    }
}
