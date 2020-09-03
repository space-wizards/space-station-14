#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body;
using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Observer;
using Content.Shared.Body.Part;
using Content.Shared.Body.Part.Properties.Movement;
using Content.Shared.Body.Part.Properties.Other;
using Content.Shared.Body.Preset;
using Content.Shared.Body.Template;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    /// <summary>
    ///     Component representing a collection of <see cref="IBodyPart"></see>
    ///     attached to each other.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(ISharedBodyManagerComponent))]
    [ComponentReference(typeof(IBodyManagerComponent))]
    public partial class BodyManagerComponent : SharedBodyManagerComponent, IBodyPartContainer, IRelayMoveInput, IBodyManagerComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IBodyNetworkFactory _bodyNetworkFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        [ViewVariables] private string _presetName = default!;

        private readonly Dictionary<string, IBodyPart> _parts = new Dictionary<string, IBodyPart>();

        [ViewVariables] private readonly Dictionary<Type, BodyNetwork> _networks = new Dictionary<Type, BodyNetwork>();

        /// <summary>
        ///     All <see cref="IBodyPart"></see> with <see cref="LegProperty"></see>
        ///     that are currently affecting move speed, mapped to how big that leg
        ///     they're on is.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<IBodyPart, float> _activeLegs = new Dictionary<IBodyPart, float>();

        [ViewVariables] public BodyTemplate Template { get; private set; } = default!;

        [ViewVariables] public BodyPreset Preset { get; private set; } = default!;

        /// <summary>
        ///     Maps <see cref="BodyTemplate"/> slot name to the <see cref="IBodyPart"/>
        ///     object filling it (if there is one).
        /// </summary>
        [ViewVariables]
        public IReadOnlyDictionary<string, IBodyPart> Parts => _parts;

        /// <summary>
        ///     List of all slots in this body, taken from the keys of
        ///     <see cref="Template"/> slots.
        /// </summary>
        public IEnumerable<string> AllSlots => Template.Slots.Keys;

        /// <summary>
        ///     List of all occupied slots in this body, taken from the values of
        ///     <see cref="Parts"/>.
        /// </summary>
        public IEnumerable<string> OccupiedSlots => Parts.Keys;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "baseTemplate",
                "bodyTemplate.Humanoid",
                template =>
                {
                    if (!_prototypeManager.TryIndex(template, out BodyTemplatePrototype templateData))
                    {
                        // Invalid prototype
                        throw new InvalidOperationException(
                            $"No {nameof(BodyTemplatePrototype)} found with name {template}");
                    }

                    Template = new BodyTemplate(templateData);
                },
                () => Template.Name);

            serializer.DataReadWriteFunction(
                "basePreset",
                "bodyPreset.BasicHuman",
                preset =>
                {
                    if (!_prototypeManager.TryIndex(preset, out BodyPresetPrototype presetData))
                    {
                        // Invalid prototype
                        throw new InvalidOperationException(
                            $"No {nameof(BodyPresetPrototype)} found with name {preset}");
                    }

                    Preset = new BodyPreset(presetData);
                },
                () => _presetName);
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadBodyPreset(Preset);
        }

        protected override void Startup()
        {
            base.Startup();

            // Just in case something activates at default health.
            ForceHealthChangedEvent();
        }

        private void LoadBodyPreset(BodyPreset preset)
        {
            _presetName = preset.Name;

            foreach (var slotName in Template.Slots.Keys)
            {
                // For each slot in our BodyManagerComponent's template,
                // try and grab what the ID of what the preset says should be inside it.
                if (!preset.PartIDs.TryGetValue(slotName, out var partId))
                {
                    // If the preset doesn't define anything for it, continue.
                    continue;
                }

                // Get the BodyPartPrototype corresponding to the BodyPart ID we grabbed.
                if (!_prototypeManager.TryIndex(partId, out BodyPartPrototype newPartData))
                {
                    throw new InvalidOperationException($"No {nameof(BodyPartPrototype)} prototype found with ID {partId}");
                }

                // Try and remove an existing limb if that exists.
                RemovePart(slotName, false);

                // Add a new BodyPart with the BodyPartPrototype as a baseline to our
                // BodyComponent.
                var addedPart = new BodyPart(newPartData);
                TryAddPart(slotName, addedPart);
            }

            OnBodyChanged(); // TODO: Duplicate code
        }

        /// <summary>
        ///     Changes the current <see cref="BodyTemplate"/> to the given
        ///     <see cref="BodyTemplate"/>.
        ///     Attempts to keep previous <see cref="IBodyPart"/> if there is a
        ///     slot for them in both <see cref="BodyTemplate"/>.
        /// </summary>
        public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate)
        {
            foreach (var part in Parts)
            {
                // TODO: Make this work.
            }

            OnBodyChanged();
        }

        /// <summary>
        ///     This method is called by <see cref="BodySystem.Update"/> before
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PreMetabolism(float frameTime)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                return;
            }

            foreach (var part in Parts.Values)
            {
                part.PreMetabolism(frameTime);
            }

            foreach (var network in _networks.Values)
            {
                network.Update(frameTime);
            }
        }

        /// <summary>
        ///     This method is called by <see cref="BodySystem.Update"/> after
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PostMetabolism(float frameTime)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                return;
            }

            foreach (var part in Parts.Values)
            {
                part.PostMetabolism(frameTime);
            }

            foreach (var network in _networks.Values)
            {
                network.Update(frameTime);
            }
        }

        /// <summary>
        ///     Called when the layout of this body changes.
        /// </summary>
        private void OnBodyChanged()
        {
            // Calculate move speed based on this body.
            if (Owner.HasComponent<MovementSpeedModifierComponent>())
            {
                _activeLegs.Clear();
                var legParts = Parts.Values.Where(x => x.HasProperty(typeof(LegProperty)));

                foreach (var part in legParts)
                {
                    var footDistance = DistanceToNearestFoot(this, part);

                    if (Math.Abs(footDistance - float.MinValue) > 0.001f)
                    {
                        _activeLegs.Add(part, footDistance);
                    }
                }

                CalculateSpeed();
            }
        }

        private void CalculateSpeed()
        {
            if (!Owner.TryGetComponent(out MovementSpeedModifierComponent? playerMover))
            {
                return;
            }

            float speedSum = 0;
            foreach (var part in _activeLegs.Keys)
            {
                if (!part.HasProperty<LegProperty>())
                {
                    _activeLegs.Remove(part);
                }
            }

            foreach (var (key, value) in _activeLegs)
            {
                if (key.TryGetProperty(out LegProperty? leg))
                {
                    // Speed of a leg = base speed * (1+log1024(leg length))
                    speedSum += leg.Speed * (1 + (float) Math.Log(value, 1024.0));
                }
            }

            if (speedSum <= 0.001f || _activeLegs.Count <= 0)
            {
                playerMover.BaseWalkSpeed = 0.8f;
                playerMover.BaseSprintSpeed = 2.0f;
            }
            else
            {
                // Extra legs stack diminishingly.
                // Final speed = speed sum/(leg count-log4(leg count))
                playerMover.BaseWalkSpeed =
                    speedSum / (_activeLegs.Count - (float) Math.Log(_activeLegs.Count, 4.0));

                playerMover.BaseSprintSpeed = playerMover.BaseWalkSpeed * 1.75f;
            }
        }

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }

        #region BodyNetwork Functions

        private bool EnsureNetwork(BodyNetwork network)
        {
            DebugTools.AssertNotNull(network);

            if (_networks.ContainsKey(network.GetType()))
            {
                return true;
            }

            _networks.Add(network.GetType(), network);
            network.OnAdd(Owner);

            return false;
        }

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to this body.
        /// </summary>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (such as passing in an invalid type or a network of that type already
        ///     existing).
        /// </returns>
        public bool EnsureNetwork(Type networkType)
        {
            DebugTools.Assert(networkType.IsSubclassOf(typeof(BodyNetwork)));

            var network = _bodyNetworkFactory.GetNetwork(networkType);
            return EnsureNetwork(network);
        }

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to
        ///     this body.
        /// </summary>
        /// <typeparam name="T">The type of network to add.</typeparam>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (such as passing in an invalid type or a network of that type already
        ///     existing).
        /// </returns>
        public bool EnsureNetwork<T>() where T : BodyNetwork
        {
            return EnsureNetwork(typeof(T));
        }

        public void RemoveNetwork(Type networkType)
        {
            DebugTools.AssertNotNull(networkType);

            if (_networks.Remove(networkType, out var network))
            {
                network.OnRemove();
            }
        }

        public void RemoveNetwork<T>() where T : BodyNetwork
        {
            RemoveNetwork(typeof(T));
        }

        /// <summary>
        ///     Attempts to get the <see cref="BodyNetwork"/> of the given type in this body.
        /// </summary>
        /// <param name="networkType">The type to search for.</param>
        /// <param name="result">
        ///     The <see cref="BodyNetwork"/> if found, null otherwise.
        /// </param>
        /// <returns>True if found, false otherwise.</returns>
        public bool TryGetNetwork(Type networkType, [NotNullWhen(true)] out BodyNetwork result)
        {
            return _networks.TryGetValue(networkType, out result!);
        }

        #endregion

        #region Recursion Functions

        /// <summary>
        ///     Returns the combined length of the distance to the nearest <see cref="BodyPart"/> with a
        ///     <see cref="FootProperty"/>. Returns <see cref="float.MinValue"/>
        ///     if there is no foot found. If you consider a <see cref="BodyManagerComponent"/> a node map, then it will look for
        ///     a foot node from the given node. It can
        ///     only search through BodyParts with <see cref="ExtensionProperty"/>.
        /// </summary>
        private static float DistanceToNearestFoot(BodyManagerComponent body, IBodyPart source)
        {
            if (source.HasProperty<FootProperty>() && source.TryGetProperty<ExtensionProperty>(out var property))
            {
                return property.ReachDistance;
            }

            return LookForFootRecursion(body, source, new List<BodyPart>());
        }

        // TODO: Make this not static and not keep me up at night
        private static float LookForFootRecursion(BodyManagerComponent body, IBodyPart current,
            ICollection<BodyPart> searchedParts)
        {
            if (!current.TryGetProperty<ExtensionProperty>(out var extProperty))
            {
                return float.MinValue;
            }

            // Get all connected parts if the current part has an extension property
            if (!body.TryGetPartConnections(current, out var connections))
            {
                return float.MinValue;
            }

            // If a connected BodyPart is a foot, return this BodyPart's length.
            foreach (var connection in connections)
            {
                if (!searchedParts.Contains(connection) && connection.HasProperty<FootProperty>())
                {
                    return extProperty.ReachDistance;
                }
            }

            // Otherwise, get the recursion values of all connected BodyParts and
            // store them in a list.
            var distances = new List<float>();
            foreach (var connection in connections)
            {
                if (!searchedParts.Contains(connection))
                {
                    continue;
                }

                var result = LookForFootRecursion(body, connection, searchedParts);

                if (Math.Abs(result - float.MinValue) > 0.001f)
                {
                    distances.Add(result);
                }
            }

            // If one or more of the searches found a foot, return the smallest one
            // and add this ones length.
            if (distances.Count > 0)
            {
                return distances.Min<float>() + extProperty.ReachDistance;
            }

            return float.MinValue;

            // No extension property, no go.
        }

        #endregion
    }

    public interface IBodyManagerHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    public class BodyManagerHealthChangeParams : HealthChangeParams, IBodyManagerHealthChangeParams
    {
        public BodyManagerHealthChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
