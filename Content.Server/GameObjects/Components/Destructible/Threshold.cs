#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Destructible;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
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
        [ViewVariables] public int? Damage;

        [ViewVariables] public Dictionary<DamageClass, int>? DamageClasses;

        [ViewVariables] public Dictionary<DamageType, int>? DamageTypes;

        [ViewVariables] public int DamageTotal =>
            Damage +
            DamageClasses?.Values.Sum() ?? 0 +
            DamageTypes?.Values.Sum() ?? 0;

        /// <summary>
        ///     Whether or not <see cref="Damage"/>, <see cref="DamageClasses"/> and
        ///     <see cref="DamageTypes"/> all have to be met in order to reach this state,
        ///     or just one of them.
        /// </summary>
        [ViewVariables] public bool Inclusive = true;

        [ViewVariables] public ThresholdAppearance? Appearance;

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
            serializer.DataField(ref Damage, "damage", null);
            serializer.DataField(ref DamageClasses, "damageClasses", null);
            serializer.DataField(ref DamageTypes, "damageTypes", null);
            serializer.DataField(ref Inclusive, "inclusive", true);
            serializer.DataField(ref Appearance, "appearance", null);
            serializer.DataField(ref Spawn, "spawn", null);
            serializer.DataField(ref Sound, "sound", string.Empty);
            serializer.DataField(ref SoundCollection, "soundCollection", string.Empty);
            serializer.DataField(ref Acts, "acts", 0, WithFormat.Flags<ActsFlags>());
            serializer.DataField(ref Triggered, "triggered", false);
            serializer.DataField(ref TriggersOnce, "triggersOnce", false);
        }

        private bool DamageClassesReached(IReadOnlyDictionary<DamageClass, int>? classesReached)
        {
            if (DamageClasses == null)
            {
                return true;
            }

            if (classesReached == null)
            {
                return false;
            }

            foreach (var (@class, damageRequired) in DamageClasses)
            {
                if (!classesReached.TryGetValue(@class, out var damageReached) ||
                    damageReached < damageRequired)
                {
                    return false;
                }
            }

            return true;
        }

        private bool DamageTypesReached(IReadOnlyDictionary<DamageType, int>? typesReached)
        {
            if (DamageTypes == null)
            {
                return true;
            }

            if (typesReached == null)
            {
                return false;
            }

            foreach (var (type, damageRequired) in DamageTypes)
            {
                if (!typesReached.TryGetValue(type, out var damageReached) ||
                    damageReached < damageRequired)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Reached(
            int? damage = null,
            IReadOnlyDictionary<DamageClass, int>? damageClasses = null,
            IReadOnlyDictionary<DamageType, int>? damageTypes = null)
        {
            if (Inclusive)
            {
                return damage >= Damage &&
                       DamageClassesReached(damageClasses) &&
                       DamageTypesReached(damageTypes);
            }
            else
            {
                return damage >= Damage ||
                       DamageClassesReached(damageClasses) ||
                       DamageTypesReached(damageTypes);
            }
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="random">
        ///     An instance of <see cref="IRobustRandom"/> to get randomness from,
        ///     if relevant.
        /// </param>
        /// <param name="actSystem">
        ///     An instance of <see cref="ActSystem"/> to call acts on, if relevant.
        /// </param>
        public void Trigger(IEntity owner, IRobustRandom random, ActSystem actSystem)
        {
            Triggered = true;

            UpdateAppearance(owner);
            PlaySound(owner);
            DoSpawn(owner, random);
            DoActs(owner, actSystem);
        }

        private void UpdateAppearance(IEntity owner)
        {
            if (Appearance == null)
            {
                return;
            }

            if (!owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                return;
            }

            // TODO Remove layers == null see https://github.com/space-wizards/RobustToolbox/pull/1461
            if (!appearanceComponent.TryGetData(DamageVisualizerData.Layers, out Dictionary<int, ThresholdAppearance>? layers) ||
                layers == null)
            {
                layers = new Dictionary<int, ThresholdAppearance>();
            }

            var appearance = Appearance.Value;
            var layerIndex = appearance.Layer ?? 0;
            layers[layerIndex] = appearance;

            appearanceComponent.SetData(DamageVisualizerData.Layers, layers);
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
