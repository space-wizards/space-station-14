#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage
    ///     and triggers thresholds when reached.
    /// </summary>
    [RegisterComponent]
    public class DestructibleComponent : Component
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private ActSystem _actSystem = default!;

        public override string Name => "Destructible";

        [ViewVariables]
        private SortedDictionary<int, Threshold> _thresholds = new SortedDictionary<int, Threshold>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "thresholds",
                new Dictionary<int, Threshold>(),
                thresholds => _thresholds = new SortedDictionary<int, Threshold>(thresholds, Comparer<int>.Create((a, b) => b.CompareTo(a))),
                () => new Dictionary<int, Threshold>(_thresholds));
        }

        public override void Initialize()
        {
            base.Initialize();

            _actSystem = EntitySystem.Get<ActSystem>();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DamageChangedMessage msg:
                {
                    if (msg.Damageable.Owner != Owner)
                    {
                        break;
                    }

                    // TODO LINQ
                    foreach (var (threshold, state) in _thresholds.Reverse())
                    {
                        if (msg.Damageable.TotalDamage >= threshold)
                        {
                            state.Trigger(Owner, _random, _actSystem);
                            break;
                        }
                    }

                    break;
                }
            }
        }

        public struct MinMax
        {
            [ViewVariables]
            public int Min;

            [ViewVariables]
            public int Max;
        }

        public struct Threshold : IExposeData
        {
            /// <summary>
            ///     Entities spawned on reaching this threshold, from a min to a max.
            /// </summary>
            [ViewVariables]
            public Dictionary<string, MinMax>? Spawn;

            /// <summary>
            ///     Sound played upon destruction.
            /// </summary>
            [ViewVariables]
            public string Sound;

            /// <summary>
            ///     Used instead of <see cref="Sound"/> if specified.
            /// </summary>
            [ViewVariables]
            public string SoundCollection;

            /// <summary>
            ///     What acts this threshold should trigger upon activation.
            ///     See <see cref="ActSystem"/>.
            /// </summary>
            [ViewVariables] public int Acts;

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref Spawn, "Spawn", null);
                serializer.DataField(ref Sound, "Sound", string.Empty);
                serializer.DataField(ref SoundCollection, "SoundCollection", string.Empty);
                serializer.DataField(ref Acts, "Acts", 0, WithFormat.Flags<ActsFlags>());
            }

            public void Trigger(IEntity owner, IRobustRandom random, ActSystem acts)
            {
                PlaySound(owner);
                DoSpawn(owner, random);
                DoActs(owner, acts);
            }

            private void PlaySound(IEntity owner)
            {
                var pos = owner.Transform.Coordinates;
                var sound = string.Empty;

                if (SoundCollection != string.Empty)
                {
                    sound = AudioHelpers.GetRandomFileFromSoundCollection(SoundCollection);
                }
                else if (Sound != string.Empty)
                {
                    sound = Sound;
                }

                if (sound != string.Empty)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(sound, pos, AudioHelpers.WithVariation(0.125f));
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

        [Flags, FlagsFor(typeof(ActsFlags))]
        [Serializable]
        public enum ThresholdActs
        {
            Breakage,
            Destruction
        }

        public sealed class ActsFlags { }
    }
}
