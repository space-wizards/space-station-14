using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class SoundAction : IInstantAction
    {
        private const float Variation = 0.125f;
        private const float Volume = 4f;

        private List<string> _sound;
        private string _backup;
        /// seconds
        private float _cooldown;

        private IRobustRandom _random;

        public SoundAction()
        {
            _random = IoCManager.Resolve<IRobustRandom>();
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _sound, "sound", null);
            serializer.DataField(ref _cooldown, "cooldown", 10);
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!ActionBlockerSystem.CanEmote(args.Performer)) return;
            if (!args.Performer.TryGetComponent<HumanoidAppearanceComponent>(out var humanoid)) return;
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;

            if (!string.IsNullOrWhiteSpace(_backup))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_backup, args.Performer, AudioParams.Default.WithVolume(Volume));
            }
            else
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_random.Pick(_sound), args.Performer,
                    AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(_cooldown));
                return;
            }
        }
    }
}
