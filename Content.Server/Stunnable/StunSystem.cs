using System;
using Content.Server.Act;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunnableComponent, DisarmedActEvent>(OnDisarmed);
        }

        private void OnDisarmed(EntityUid uid, StunnableComponent stunnable, DisarmedActEvent args)
        {
            if (args.Handled || !_random.Prob(args.PushProbability))
                return;

            Paralyze(uid, TimeSpan.FromSeconds(4f), stunnable);

            var source = args.Source;
            var target = args.Target;

            if (source != null)
            {
                SoundSystem.Play(Filter.Pvs(source), stunnable.StunAttemptSound.GetSound(), source, AudioHelpers.WithVariation(0.025f));

                if (target != null)
                {
                    // TODO: Use PopupSystem
                    source.PopupMessageOtherClients(Loc.GetString("stunnable-component-disarm-success-others", ("source", source.Name), ("target", target.Name)));
                    source.PopupMessageCursor(Loc.GetString("stunnable-component-disarm-success", ("target", target.Name)));
                }
            }

            args.Handled = true;
        }
    }
}
