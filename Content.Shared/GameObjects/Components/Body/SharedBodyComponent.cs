#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Part.Property.Movement;
using Content.Shared.GameObjects.Components.Body.Part.Property.Other;
using Content.Shared.GameObjects.Components.Body.Preset;
using Content.Shared.GameObjects.Components.Body.Template;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body
{
    public abstract class SharedBodyComponent : DamageableComponent, IBody
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Body";

        public override uint? NetID => ContentNetIDs.BODY;

        private string? _centerSlot;

        private Dictionary<string, string> _partIds = new Dictionary<string, string>();

        private readonly Dictionary<string, IBodyPart> _parts = new Dictionary<string, IBodyPart>();

        /// <summary>
        ///     All <see cref="IBodyPart"></see> with <see cref="LegProperty"></see>
        ///     that are currently affecting move speed, mapped to how big that leg
        ///     they're on is.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<IBodyPart, float> _activeLegs = new Dictionary<IBodyPart, float>();

        [ViewVariables] public string? TemplateName { get; private set; }

        [ViewVariables] public string? PresetName { get; private set; }

        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots { get; private set; } = new Dictionary<string, BodyPartType>();

        [ViewVariables]
        public Dictionary<string, List<string>> Connections { get; private set; } = new Dictionary<string, List<string>>();

        /// <summary>
        ///     Maps slots to the part filling each one.
        /// </summary>
        [ViewVariables]
        public IReadOnlyDictionary<string, IBodyPart> Parts => _parts;

        /// <summary>
        ///     List of all occupied slots in this body, taken from the values of
        ///     <see cref="Parts"/>.
        /// </summary>
        public IEnumerable<string> OccupiedSlots => Parts.Keys;

        /// <summary>
        ///     List of all slots in this body.
        /// </summary>
        public IEnumerable<string> AllSlots => Slots.Keys;

        public IReadOnlyDictionary<string, string> PartIds => _partIds;

        [ViewVariables] public IReadOnlyDictionary<string, string> PartIDs => _partIds;

        public bool TryAddPart(string slot, IBodyPart part, bool force = false)
        {
            DebugTools.AssertNotNull(part);
            DebugTools.AssertNotNull(slot);

            // Make sure the given slot exists
            if (force)
            {
                if (!HasSlot(slot))
                {
                    Slots[slot] = part.PartType;
                }

                _parts[slot] = part;
            }
            else
            {
                // And that nothing is in it
                if (!_parts.TryAdd(slot, part))
                {
                    return false;
                }
            }

            part.Owner.Transform.AttachParent(Owner);
            part.Body = this;

            var argsAdded = new BodyPartAddedEventArgs(part, slot);

            foreach (var component in Owner.GetAllComponents<IBodyPartAdded>().ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }

            // TODO: Sort this duplicate out
            OnBodyChanged();

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

            if (string.IsNullOrEmpty(slotName))
            {
                return;
            }

            RemovePart(slotName, drop);
        }

        // TODO invert this behavior with the one above
        public bool RemovePart(string slot, bool drop)
        {
            DebugTools.AssertNotNull(slot);

            if (!_parts.Remove(slot, out var part))
            {
                return false;
            }

            if (drop)
            {
                part.Drop();
            }

            // TODO BODY Move to Body part
            if (!part.Owner.Transform.Deleted)
            {
                part.Owner.Transform.AttachParent(Owner);
            }

            part.Body = null;

            var args = new BodyPartRemovedEventArgs(part, slot);

            foreach (var component in Owner.GetAllComponents<IBodyPartRemoved>())
            {
                component.BodyPartRemoved(args);
            }

            if (CurrentDamageState == DamageState.Dead) return true;

            // creadth: fall down if no legs
            if (part.PartType == BodyPartType.Leg && Parts.Count(x => x.Value.PartType == BodyPartType.Leg) == 0)
            {
                EntitySystem.Get<SharedStandingStateSystem>().Down(Owner);
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

        public bool RemovePart(IBodyPart part, [NotNullWhen(true)] out string? slotName)
        {
            DebugTools.AssertNotNull(part);

            var pair = _parts.FirstOrDefault(kvPair => kvPair.Value == part);

            if (pair.Equals(default))
            {
                slotName = null;
                return false;
            }

            if (RemovePart(pair.Key, false))
            {
                slotName = pair.Key;
                return true;
            }

            slotName = null;
            return false;
        }

        public bool TryDropPart(IBodyPart part, [NotNullWhen(true)] out List<IBodyPart>? dropped)
        {
            DebugTools.AssertNotNull(part);

            if (!_parts.ContainsValue(part))
            {
                dropped = null;
                return false;
            }

            if (!RemovePart(part, out var slotName))
            {
                dropped = null;
                return false;
            }

            part.Drop();

            dropped = new List<IBodyPart> {part};
            // Call disconnect on all limbs that were hanging off this limb.
            if (TryGetSlotConnections(slotName, out var connections))
            {
                // This loop is an unoptimized travesty. TODO: optimize to be less shit
                foreach (var connectionName in connections)
                {
                    if (TryGetPart(connectionName, out var result) &&
                        !ConnectedToCenter(result) &&
                        RemovePart(connectionName, true))
                    {
                        dropped.Add(result);
                    }
                }
            }

            OnBodyChanged();
            return true;
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
            if (_centerSlot == null) return null;

            return Parts.GetValueOrDefault(_centerSlot);
        }

        public bool HasSlot(string slot)
        {
            return Slots.ContainsKey(slot);
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
            return Slots.TryGetValue(slot, out result);
        }

        public bool TryGetSlotConnections(string slot, [NotNullWhen(true)] out List<string>? connections)
        {
            return Connections.TryGetValue(slot, out connections);
        }

        public bool TryGetPartConnections(string slot, [NotNullWhen(true)] out List<IBodyPart>? connections)
        {
            if (!Connections.TryGetValue(slot, out var slotConnections))
            {
                connections = null;
                return false;
            }

            connections = new List<IBodyPart>();
            foreach (var connection in slotConnections)
            {
                if (TryGetPart(connection, out var part))
                {
                    connections.Add(part);
                }
            }

            if (connections.Count <= 0)
            {
                connections = null;
                return false;
            }

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
                var legParts = Parts.Values.Where(x => x.HasProperty<LegProperty>());

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
        ///     Returns the combined length of the distance to the nearest
        ///     <see cref="IBodyPart"/> that is a foot.
        ///     If you consider a <see cref="IBody"/> a node map, then it will
        ///     look for a foot node from the given node. It can only search
        ///     through <see cref="IBodyPart"/>s with an
        ///     <see cref="ExtensionProperty"/>.
        /// </summary>
        /// <returns>
        ///     The distance to the foot if found, <see cref="float.MinValue"/>
        ///     otherwise.
        /// </returns>
        public float DistanceToNearestFoot(IBodyPart source)
        {
            if (source.PartType == BodyPartType.Foot &&
                source.TryGetProperty<ExtensionProperty>(out var extension))
            {
                return extension.ReachDistance;
            }

            return LookForFootRecursion(source, new List<IBodyPart>());
        }

        private float LookForFootRecursion(IBodyPart current, ICollection<IBodyPart> searchedParts)
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
                if (connection.PartType == BodyPartType.Foot &&
                    !searchedParts.Contains(connection))
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
        }

        // TODO BODY optimize this
        public KeyValuePair<string, BodyPartType> SlotAt(int index)
        {
            return Slots.ElementAt(index);
        }

        public KeyValuePair<string, IBodyPart> PartAt(int index)
        {
            return Parts.ElementAt(index);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "template",
                null,
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return;
                    }

                    var template = _prototypeManager.Index<BodyTemplatePrototype>(name);

                    Connections = template.Connections;
                    Slots = template.Slots;
                    _centerSlot = template.CenterSlot;

                    TemplateName = name;
                },
                () => TemplateName);

            serializer.DataReadWriteFunction(
                "preset",
                null,
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return;
                    }

                    var preset = _prototypeManager.Index<BodyPresetPrototype>(name);

                    _partIds = preset.PartIDs;
                },
                () => PresetName);

            serializer.DataReadWriteFunction(
                "connections",
                new Dictionary<string, List<string>>(),
                connections =>
                {
                    foreach (var (from, to) in connections)
                    {
                        Connections.GetOrNew(from).AddRange(to);
                    }
                },
                () => Connections);

            serializer.DataReadWriteFunction(
                "slots",
                new Dictionary<string, BodyPartType>(),
                slots =>
                {
                    foreach (var (part, type) in slots)
                    {
                        Slots[part] = type;
                    }
                },
                () => Slots);

            // TODO
            serializer.DataReadWriteFunction(
                "centerSlot",
                null,
                slot => _centerSlot = slot,
                () => _centerSlot);

            serializer.DataReadWriteFunction(
                "partIds",
                new Dictionary<string, string>(),
                partIds =>
                {
                    foreach (var (slot, part) in partIds)
                    {
                        _partIds[slot] = part;
                    }
                },
                () => _partIds);

            // Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
            // The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
            var cleanedConnections = new Dictionary<string, List<string>>();
            foreach (var targetSlotName in Slots.Keys)
            {
                var tempConnections = new List<string>();
                foreach (var (slotName, slotConnections) in Connections)
                {
                    if (slotName == targetSlotName)
                    {
                        foreach (var connection in slotConnections)
                        {
                            if (!tempConnections.Contains(connection))
                            {
                                tempConnections.Add(connection);
                            }
                        }
                    }
                    else if (slotConnections.Contains(targetSlotName))
                    {
                        tempConnections.Add(slotName);
                    }
                }

                if (tempConnections.Count > 0)
                {
                    cleanedConnections.Add(targetSlotName, tempConnections);
                }
            }

            Connections = cleanedConnections;
        }

        protected override void Startup()
        {
            base.Startup();

            // Just in case something activates at default health.
            ForceHealthChangedEvent();
        }
    }
}
