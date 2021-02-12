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
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class ScreamAction : IInstantAction
    {
        private const float Variation = 0.125f;
        private const float Volume = 4f;

        private List<string> _male;
        private List<string> _female;
        private string _wilhelm;
        /// seconds
        private float _cooldown;

        private IRobustRandom _random;

        public ScreamAction()
        {
            _random = IoCManager.Resolve<IRobustRandom>();
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _male, "male", null);
            serializer.DataField(ref _female, "female", null);
            serializer.DataField(ref _wilhelm, "wilhelm", null);
            serializer.DataField(ref _cooldown, "cooldown", 10);
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!ActionBlockerSystem.CanSpeak(args.Performer)) return;
            if (!args.Performer.TryGetComponent<HumanoidAppearanceComponent>(out var humanoid)) return;
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;

            if (_random.Prob(.01f) && !string.IsNullOrWhiteSpace(_wilhelm))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_wilhelm, args.Performer, AudioParams.Default.WithVolume(Volume));
            }
            else
            {
                switch (humanoid.Sex)
                {
                    case Sex.Male:
                        if (_male == null) break;
                        EntitySystem.Get<AudioSystem>().PlayFromEntity(_random.Pick(_male), args.Performer,
                            AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                        break;
                    case Sex.Female:
                        if (_female == null) break;
                        EntitySystem.Get<AudioSystem>().PlayFromEntity(_random.Pick(_female), args.Performer,
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
