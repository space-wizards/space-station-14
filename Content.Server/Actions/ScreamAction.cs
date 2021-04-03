#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Preferences;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ScreamAction : IInstantAction
    {
        private const float Variation = 0.125f;
        private const float Volume = 4f;

        [Dependency] private readonly IRobustRandom _random = default!;

        [DataField("male")] private List<string>? _male;
        [DataField("female")] private List<string>? _female;
        [DataField("wilhelm")] private string? _wilhelm;

        /// seconds
        [DataField("cooldown")] private float _cooldown = 10;

        public ScreamAction()
        {
            IoCManager.InjectDependencies(this);
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!ActionBlockerSystem.CanSpeak(args.Performer)) return;
            if (!args.Performer.TryGetComponent<HumanoidAppearanceComponent>(out var humanoid)) return;
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;

            if (_random.Prob(.01f) && !string.IsNullOrWhiteSpace(_wilhelm))
            {
                SoundSystem.Play(Filter.Pvs(args.Performer), _wilhelm, args.Performer, AudioParams.Default.WithVolume(Volume));
            }
            else
            {
                switch (humanoid.Sex)
                {
                    case Sex.Male:
                        if (_male == null) break;
                        SoundSystem.Play(Filter.Pvs(args.Performer), _random.Pick(_male), args.Performer,
                            AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                        break;
                    case Sex.Female:
                        if (_female == null) break;
                        SoundSystem.Play(Filter.Pvs(args.Performer), _random.Pick(_female), args.Performer,
                            AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }



            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(_cooldown));
        }
    }
}
