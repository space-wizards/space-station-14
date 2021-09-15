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

            SubscribeLocalEvent<DiceComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<DiceComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DiceComponent, LandEvent>(OnLand);
            SubscribeLocalEvent<DiceComponent, ExaminedEvent>(OnExamined);
        }

        public void Roll(EntityUid uid, DiceComponent die)
        {
            die.CurrentSide = _random.Next(1, (die.Sides/die.Step)+1) * die.Step;

            SoundSystem.Play(Filter.Pvs(die.Owner), die.Sound.GetSound(), die.Owner, AudioHelpers.WithVariation(0.05f));

            if (!ComponentManager.TryGetComponent(uid, out SpriteComponent? sprite))
                return;

            // TODO DICE: Use a visualizer instead.
            sprite.LayerSetState(0, $"d{die.Sides}{die.CurrentSide}");
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
    }
}
