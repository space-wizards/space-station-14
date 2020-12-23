#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible
{
    public class Threshold : IExposeData
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [ViewVariables] public Dictionary<string, MinMax>? Spawn;

        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [ViewVariables] public string Sound = string.Empty;

        /// <summary>
        ///     Used instead of <see cref="Sound"/> if specified.
        /// </summary>
        [ViewVariables] public string SoundCollection = string.Empty;

        /// <summary>
        ///     What acts this threshold should trigger upon activation.
        ///     See <see cref="ActSystem"/>.
        /// </summary>
        [ViewVariables] public int Acts;

        /// <summary>
        ///     Whether or not this threshold has already been triggered.
        /// </summary>
        [ViewVariables] public bool Triggered;

        /// <summary>
        ///     Whether or not this threshold only triggers once.
        ///     If false, it will trigger again once the entity is healed
        ///     and then damaged to reach this threshold once again.
        ///     It will not repeatedly trigger as damage rises beyond that.
        /// </summary>
        [ViewVariables] public bool TriggersOnce;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Spawn, "spawn", null);
            serializer.DataField(ref Sound, "sound", string.Empty);
            serializer.DataField(ref SoundCollection, "soundCollection", string.Empty);
            serializer.DataField(ref Acts, "acts", 0, WithFormat.Flags<ActsFlags>());
            serializer.DataField(ref Triggered, "triggered", false);
            serializer.DataField(ref TriggersOnce, "triggersOnce", false);
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="random">
        ///     An instance of <see cref="IRobustRandom"/> to get randomness from, if relevant.
        /// </param>
        /// <param name="actSystem">
        ///     An instance of <see cref="ActSystem"/> to call acts on, if relevant.
        /// </param>
        public void Trigger(IEntity owner, IRobustRandom random, ActSystem actSystem)
        {
            Triggered = true;

            PlaySound(owner);
            DoSpawn(owner, random);
            DoActs(owner, actSystem);
        }

        private void PlaySound(IEntity owner)
        {
            var pos = owner.Transform.Coordinates;
            var actualSound = string.Empty;

            if (SoundCollection != string.Empty)
            {
                actualSound = AudioHelpers.GetRandomFileFromSoundCollection(SoundCollection);
            }
            else if (Sound != string.Empty)
            {
                actualSound = Sound;
            }

            if (actualSound != string.Empty)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(actualSound, pos, AudioHelpers.WithVariation(0.125f));
            }
        }

        private void DoSpawn(IEntity owner, IRobustRandom random)
        {
            if (Spawn == null)
            {
                return;
            }

            foreach (var (key, value) in Spawn)
            {
                var count = value.Min >= value.Max
                    ? value.Min
                    : random.Next(value.Min, value.Max + 1);

                if (count == 0) continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(key))
                {
                    var spawned = owner.EntityManager.SpawnEntity(key, owner.Transform.Coordinates);
                    var stack = spawned.GetComponent<StackComponent>();
                    stack.Count = count;
                    spawned.RandomOffset(0.5f);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = owner.EntityManager.SpawnEntity(key, owner.Transform.Coordinates);
                        spawned.RandomOffset(0.5f);
                    }
                }
            }
        }

        private void DoActs(IEntity owner, ActSystem acts)
        {
            if ((Acts & (int) ThresholdActs.Breakage) != 0)
            {
                acts.HandleBreakage(owner);
            }

            if ((Acts & (int) ThresholdActs.Destruction) != 0)
            {
                acts.HandleDestruction(owner);
            }
        }
    }
}
