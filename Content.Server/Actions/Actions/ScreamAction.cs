using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Components;
using Content.Shared.Audio;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Cooldown;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using System;
using Robust.Shared.Serialization;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ScreamAction : IInstantAction, ISerializationHooks
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("male", required: true)] private SoundSpecifier _male = default!;
        [DataField("female", required: true)] private SoundSpecifier _female = default!;
        [DataField("wilhelm", required: true)] private SoundSpecifier _wilhelm = default!;

        [DataField("volume")] private float Volume = 4f;
        [DataField("variation")] private float Variation = 0.125f;

        /// seconds
        [DataField("cooldown")] private float _cooldown = 10;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanSpeak(args.Performer)) return;
            if (!_entMan.TryGetComponent<SharedActionsComponent?>(args.Performer, out var actions)) return;
            if (_entMan.TryGetComponent<HumanoidAppearanceComponent?>(args.Performer, out var humanoid))
            {
                if (_random.Prob(.01f))
                {
                    SoundSystem.Play(Filter.Pvs(args.Performer), _wilhelm.GetSound(), args.Performer, AudioParams.Default.WithVolume(Volume));
                }
                else
                {
                    switch (humanoid.Sex)
                    {
                        case Sex.Male:
                            SoundSystem.Play(Filter.Pvs(args.Performer), _male.GetSound(), args.Performer, AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                            break;
                        case Sex.Female:
                            SoundSystem.Play(Filter.Pvs(args.Performer), _female.GetSound(), args.Performer, AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else //non-humanoids (sexless creatures) use male screams by default
            {
                SoundSystem.Play(Filter.Pvs(args.Performer), _male.GetSound(), args.Performer, AudioHelpers.WithVariation(Variation).WithVolume(Volume));
            }

            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(_cooldown));
        }
    }
}
