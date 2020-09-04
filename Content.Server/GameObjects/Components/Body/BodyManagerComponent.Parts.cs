#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Body.Part.Properties.Movement;
using Content.Shared.Body.Part.Properties.Other;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    public partial class BodyManagerComponent
    {
        private readonly Dictionary<string, IBodyPart> _parts = new Dictionary<string, IBodyPart>();

        [ViewVariables] public BodyPreset Preset { get; private set; } = default!;

        /// <summary>
        ///     All <see cref="IBodyPart"></see> with <see cref="LegProperty"></see>
        ///     that are currently affecting move speed, mapped to how big that leg
        ///     they're on is.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<IBodyPart, float> _activeLegs = new Dictionary<IBodyPart, float>();

        /// <summary>
        ///     Maps <see cref="BodyTemplate"/> slot name to the <see cref="IBodyPart"/>
        ///     object filling it (if there is one).
        /// </summary>
        [ViewVariables]
        public IReadOnlyDictionary<string, IBodyPart> Parts => _parts;

        /// <summary>
        ///     List of all occupied slots in this body, taken from the values of
        ///     <see cref="Parts"/>.
        /// </summary>
        public IEnumerable<string> OccupiedSlots => Parts.Keys;

        /// <summary>
        ///     List of all slots in this body, taken from the keys of
        ///     <see cref="Template"/> slots.
        /// </summary>
        public IEnumerable<string> AllSlots => Template.Slots.Keys;

        public bool TryAddPart(string slot, DroppedBodyPartComponent part, bool force = false)
        {
            DebugTools.AssertNotNull(part);

            if (!TryAddPart(slot, part.ContainedBodyPart, force))
            {
                return false;
            }

            part.Owner.Delete();
            return true;
        }

        public bool TryAddPart(string slot, IBodyPart part, bool force = false)
        {
            DebugTools.AssertNotNull(part);
            DebugTools.AssertNotNull(slot);

            // Make sure the given slot exists
            if (!force)
            {
                if (!HasSlot(slot))
                {
                    return false;
                }

                // And that nothing is in it
                if (!_parts.TryAdd(slot, part))
                {
                    return false;
                }
            }
            else
            {
                _parts[slot] = part;
            }

            part.Body = this;

            var argsAdded = new BodyPartAddedEventArgs(part, slot);

            foreach (var component in Owner.GetAllComponents<IBodyPartAdded>().ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }

            // TODO: Sort this duplicate out
            OnBodyChanged();

            if (!Template.Layers.TryGetValue(slot, out var partMap) ||
                !_reflectionManager.TryParseEnumReference(partMap, out var partEnum))
            {
                Logger.Warning($"Template {Template.Name} has an invalid RSI map key {partMap} for body part {part.Name}.");
                return false;
            }

            part.RSIMap = partEnum;

            var partMessage = new BodyPartAddedMessage(part.RSIPath, part.RSIState, partEnum);

            SendNetworkMessage(partMessage);

            foreach (var mechanism in part.Mechanisms)
            {
                if (!Template.MechanismLayers.TryGetValue(mechanism.Id, out var mechanismMap))
                {
                    continue;
                }

                if (!_reflectionManager.TryParseEnumReference(mechanismMap, out var mechanismEnum))
                {
                    Logger.Warning($"Template {Template.Name} has an invalid RSI map key {mechanismMap} for mechanism {mechanism.Id}.");
                    continue;
                }

                var mechanismMessage = new MechanismSpriteAddedMessage(mechanismEnum);

                SendNetworkMessage(mechanismMessage);
            }

            return true;
        }

        public bool HasPart(string slot)
        {
            return _parts.ContainsKey(slot);
        }

        public void RemovePart(IBodyPart part, bool drop)
        {
            DebugTools.AssertNotNull(part);

            var slotName = _parts.FirstOrDefault(x => x.Value == part).Key;

            if (string.IsNullOrEmpty(slotName)) return;

            RemovePart(slotName, drop);
        }

        public bool RemovePart(string slot, bool drop)
        {
            DebugTools.AssertNotNull(slot);

            if (!_parts.Remove(slot, out var part))
            {
                return false;
            }

            IEntity? dropped = null;
            if (drop)
            {
                part.SpawnDropped(out dropped);
            }

            part.Body = null;

            var args = new BodyPartRemovedEventArgs(part, slot);

            foreach (var component in Owner.GetAllComponents<IBodyPartRemoved>())
            {
                component.BodyPartRemoved(args);
            }

            if (part.RSIMap != null)
            {
                var message = new BodyPartRemovedMessage(part.RSIMap, dropped?.Uid);
                SendNetworkMessage(message);
            }

            foreach (var mechanism in part.Mechanisms)
            {
                if (!Template.MechanismLayers.TryGetValue(mechanism.Id, out var mechanismMap))
                {
                    continue;
                }

                if (!_reflectionManager.TryParseEnumReference(mechanismMap, out var mechanismEnum))
                {
                    Logger.Warning($"Template {Template.Name} has an invalid RSI map key {mechanismMap} for mechanism {mechanism.Id}.");
                    continue;
                }

                var mechanismMessage = new MechanismSpriteRemovedMessage(mechanismEnum);

                SendNetworkMessage(mechanismMessage);
            }

            if (CurrentDamageState == DamageState.Dead) return true;

            // creadth: fall down if no legs
            if (part.PartType == BodyPartType.Leg && Parts.Count(x => x.Value.PartType == BodyPartType.Leg) == 0)
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner);
            }

            // creadth: immediately kill entity if last vital part removed
            if (part.IsVital && Parts.Count(x => x.Value.PartType == part.PartType) == 0)
            {
                CurrentDamageState = DamageState.Dead;
                ForceHealthChangedEvent();
            }

            if (TryGetSlotConnections(slot, out var connections))
            {
                foreach (var connectionName in connections)
                {
                    if (TryGetPart(connectionName, out var result) && !ConnectedToCenter(result))
                    {
                        RemovePart(connectionName, drop);
                    }
                }
            }

            OnBodyChanged();
            return true;
        }

        public bool RemovePart(IBodyPart part, [NotNullWhen(true)] out string? slot)
        {
            DebugTools.AssertNotNull(part);

            var pair = _parts.FirstOrDefault(kvPair => kvPair.Value == part);

            if (pair.Equals(default))
            {
                slot = null;
                return false;
            }

            slot = pair.Key;

            return RemovePart(slot, false);
        }

        public IEntity? DropPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            if (!_parts.ContainsValue(part))
            {
                return null;
            }

            if (!RemovePart(part, out var slotName))
            {
                return null;
            }

            // Call disconnect on all limbs that were hanging off this limb.
            if (TryGetSlotConnections(slotName, out var connections))
            {
                // This loop is an unoptimized travesty. TODO: optimize to be less shit
                foreach (var connectionName in connections)
                {
                    if (TryGetPart(connectionName, out var result) && !ConnectedToCenter(result))
                    {
                        RemovePart(connectionName, true);
                    }
                }
            }

            part.SpawnDropped(out var dropped);

            OnBodyChanged();
            return dropped;
        }

        public bool ConnectedToCenter(IBodyPart part)
        {
            var searchedSlots = new List<string>();

            return TryGetSlot(part, out var result) &&
                   ConnectedToCenterPartRecursion(searchedSlots, result);
        }

        private bool ConnectedToCenterPartRecursion(ICollection<string> searchedSlots, string slotName)
        {
            if (!TryGetPart(slotName, out var part))
            {
                return false;
            }

            if (part == CenterPart())
            {
                return true;
            }

            searchedSlots.Add(slotName);

            if (!TryGetSlotConnections(slotName, out var connections))
            {
                return false;
            }

            foreach (var connection in connections)
            {
                if (!searchedSlots.Contains(connection) &&
                    ConnectedToCenterPartRecursion(searchedSlots, connection))
                {
                    return true;
                }
            }

            return false;
        }

        public IBodyPart? CenterPart()
        {
            Parts.TryGetValue(Template.CenterSlot, out var center);
            return center;
        }

        public bool HasSlot(string slot)
        {
            return Template.HasSlot(slot);
        }

        public bool TryGetPart(string slot, [NotNullWhen(true)] out IBodyPart? result)
        {
            return Parts.TryGetValue(slot, out result);
        }

        public bool TryGetSlot(IBodyPart part, [NotNullWhen(true)] out string? slot)
        {
            // We enforce that there is only one of each value in the dictionary,
            // so we can iterate through the dictionary values to get the key from there.
            var pair = Parts.FirstOrDefault(x => x.Value == part);
            slot = pair.Key;

            return !pair.Equals(default);
        }

        public bool TryGetSlotType(string slot, out BodyPartType result)
        {
            return Template.Slots.TryGetValue(slot, out result);
        }

        public bool TryGetSlotConnections(string slot, [NotNullWhen(true)] out List<string>? connections)
        {
            return Template.Connections.TryGetValue(slot, out connections);
        }

        public bool TryGetPartConnections(string slot, [NotNullWhen(true)] out List<IBodyPart>? result)
        {
            result = null;

            if (!Template.Connections.TryGetValue(slot, out var connections))
            {
                return false;
            }

            var toReturn = new List<IBodyPart>();
            foreach (var connection in connections)
            {
                if (TryGetPart(connection, out var partResult))
                {
                    toReturn.Add(partResult);
                }
            }

            if (toReturn.Count <= 0)
            {
                return false;
            }

            result = toReturn;
            return true;
        }

        public bool TryGetPartConnections(IBodyPart part, [NotNullWhen(true)] out List<IBodyPart>? connections)
        {
            connections = null;

            return TryGetSlot(part, out var slotName) &&
                   TryGetPartConnections(slotName, out connections);
        }

        public List<IBodyPart> GetPartsOfType(BodyPartType type)
        {
            var toReturn = new List<IBodyPart>();

            foreach (var part in Parts.Values)
            {
                if (part.PartType == type)
                {
                    toReturn.Add(part);
                }
            }

            return toReturn;
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
                    var footDistance = DistanceToNearestFoot(part);

                    if (Math.Abs(footDistance - float.MinValue) > 0.001f)
                    {
                        _activeLegs.Add(part, footDistance);
                    }
                }

                CalculateSpeed();
            }
        }

        /// <summary>
        ///     Returns the combined length of the distance to the nearest <see cref="BodyPart"/> with a
        ///     <see cref="FootProperty"/>. Returns <see cref="float.MinValue"/>
        ///     if there is no foot found. If you consider a <see cref="BodyManagerComponent"/> a node map, then it will look for
        ///     a foot node from the given node. It can
        ///     only search through BodyParts with <see cref="ExtensionProperty"/>.
        /// </summary>
        public float DistanceToNearestFoot(IBodyPart source)
        {
            if (source.HasProperty<FootProperty>() && source.TryGetProperty<ExtensionProperty>(out var property))
            {
                return property.ReachDistance;
            }

            return LookForFootRecursion(source, new List<BodyPart>());
        }

        private float LookForFootRecursion(IBodyPart current,
            ICollection<BodyPart> searchedParts)
        {
            if (!current.TryGetProperty<ExtensionProperty>(out var extProperty))
            {
                return float.MinValue;
            }

            // Get all connected parts if the current part has an extension property
            if (!TryGetPartConnections(current, out var connections))
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

                var result = LookForFootRecursion(connection, searchedParts);

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
    }
}
