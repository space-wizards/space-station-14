#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Part.Property;
using Content.Shared.GameObjects.Components.Body.Preset;
using Content.Shared.GameObjects.Components.Body.Slot;
using Content.Shared.GameObjects.Components.Body.Template;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body
{
    // TODO BODY Damage methods for collections of IDamageableComponents
    public abstract class SharedBodyComponent : Component, IBody, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Body";

        public override uint? NetID => ContentNetIDs.BODY;

        [ViewVariables]
        [field: DataField("template", required: true)]
        private string? TemplateId { get; } = default;

        [ViewVariables]
        [field: DataField("preset", required: true)]
        private string? PresetId { get; } = default;

        [ViewVariables]
        public BodyTemplatePrototype? Template => TemplateId == null
            ? null
            : _prototypeManager.Index<BodyTemplatePrototype>(TemplateId);

        [ViewVariables]
        public BodyPresetPrototype? Preset => PresetId == null
            ? null
            : _prototypeManager.Index<BodyPresetPrototype>(PresetId);

        [ViewVariables]
        private Dictionary<string, BodyPartSlot> SlotIds { get; } = new();

        [ViewVariables]
        private Dictionary<IBodyPart, BodyPartSlot> SlotParts { get; } = new();

        [ViewVariables]
        public IEnumerable<BodyPartSlot> Slots => SlotIds.Values;

        [ViewVariables]
        public IEnumerable<KeyValuePair<IBodyPart, BodyPartSlot>> Parts => SlotParts;

        [ViewVariables]
        public IEnumerable<BodyPartSlot> EmptySlots => Slots.Where(slot => slot.Part == null);

        public BodyPartSlot? CenterSlot =>
            Template?.CenterSlot is { } centerSlot
                ? SlotIds.GetValueOrDefault(centerSlot)
                : null;

        public IBodyPart? CenterPart => CenterSlot?.Part;

        public override void Initialize()
        {
            base.Initialize();

            // TODO BODY BeforeDeserialization
            // TODO BODY Move to template or somewhere else
            if (TemplateId != null)
            {
                var template = _prototypeManager.Index<BodyTemplatePrototype>(TemplateId);

                foreach (var (id, partType) in template.Slots)
                {
                    SetSlot(id, partType);
                }

                foreach (var (slotId, connectionIds) in template.Connections)
                {
                    var connections = connectionIds.Select(id => SlotIds[id]);
                    SlotIds[slotId].SetConnectionsInternal(connections);
                }
            }

            CalculateSpeed();
        }

        public override void OnRemove()
        {
            foreach (var slot in SlotIds.Values)
            {
                slot.Shutdown();
            }

            base.OnRemove();
        }

        private BodyPartSlot SetSlot(string id, BodyPartType type)
        {
            var slot = new BodyPartSlot(id, type);

            SlotIds[id] = slot;
            slot.PartAdded += part => OnAddPart(slot, part);
            slot.PartRemoved += part => OnRemovePart(slot, part);

            return slot;
        }

        private Dictionary<BodyPartSlot, IBodyPart> GetHangingParts(BodyPartSlot from)
        {
            var hanging = new Dictionary<BodyPartSlot, IBodyPart>();

            foreach (var connection in from.Connections)
            {
                if (connection.Part != null &&
                    !ConnectedToCenter(connection.Part))
                {
                    hanging.Add(connection, connection.Part);
                }
            }

            return hanging;
        }

        protected virtual bool CanAddPart(string slotId, IBodyPart part)
        {
            if (!SlotIds.TryGetValue(slotId, out var slot) ||
                slot.CanAddPart(part))
            {
                return false;
            }

            return true;
        }

        protected virtual void OnAddPart(BodyPartSlot slot, IBodyPart part)
        {
            SlotParts[part] = slot;
            part.Body = this;

            var argsAdded = new BodyPartAddedEventArgs(slot.Id, part);

            foreach (var component in Owner.GetAllComponents<IBodyPartAdded>().ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }

            // TODO BODY Sort this duplicate out
            OnBodyChanged();
        }

        protected virtual void OnRemovePart(BodyPartSlot slot, IBodyPart part)
        {
            SlotParts.Remove(part);

            foreach (var connectedSlot in slot.Connections)
            {
                if (connectedSlot.Part != null &&
                    !ConnectedToCenter(connectedSlot.Part))
                {
                    RemovePart(connectedSlot.Part);
                }
            }

            part.Body = null;

            var args = new BodyPartRemovedEventArgs(slot.Id, part);

            foreach (var component in Owner.GetAllComponents<IBodyPartRemoved>())
            {
                component.BodyPartRemoved(args);
            }

            // creadth: fall down if no legs
            if (part.PartType == BodyPartType.Leg &&
                GetPartsOfType(BodyPartType.Leg).ToArray().Length == 0)
            {
                EntitySystem.Get<SharedStandingStateSystem>().Down(Owner);
            }

            // creadth: immediately kill entity if last vital part removed
            if (Owner.TryGetComponent(out IDamageableComponent? damageable))
            {
                if (part.IsVital && SlotParts.Count(x => x.Value.PartType == part.PartType) == 0)
                {
                    damageable.ChangeDamage(DamageType.Bloodloss, 300, true); // TODO BODY KILL
                }
            }

            OnBodyChanged();
        }

        public bool TryAddPart(string slotId, IBodyPart part)
        {
            DebugTools.AssertNotNull(part);
            DebugTools.AssertNotNull(slotId);

            if (!CanAddPart(slotId, part))
            {
                return false;
            }

            return SlotIds.TryGetValue(slotId, out var slot) &&
                   slot.TryAddPart(part);
        }

        public void SetPart(string slotId, IBodyPart part)
        {
            if (!SlotIds.TryGetValue(slotId, out var slot))
            {
                slot = SetSlot(slotId, part.PartType);
                SlotIds[slotId] = slot;
            }

            slot.SetPart(part);
        }

        public bool HasPart(string slotId)
        {
            DebugTools.AssertNotNull(slotId);

            return SlotIds.TryGetValue(slotId, out var slot) &&
                   slot.Part != null;
        }

        public bool HasPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            return SlotParts.ContainsKey(part);
        }

        public bool RemovePart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            return SlotParts.TryGetValue(part, out var slot) &&
                   slot.RemovePart();
        }

        public bool RemovePart(string slotId)
        {
            DebugTools.AssertNotNull(slotId);

            return SlotIds.TryGetValue(slotId, out var slot) &&
                   slot.RemovePart();
        }

        public bool RemovePart(IBodyPart part, [NotNullWhen(true)] out BodyPartSlot? slotId)
        {
            DebugTools.AssertNotNull(part);

            if (!SlotParts.TryGetValue(part, out var slot))
            {
                slotId = null;
                return false;
            }

            if (!slot.RemovePart())
            {
                slotId = null;
                return false;
            }

            slotId = slot;
            return true;
        }

        public bool TryDropPart(BodyPartSlot slot, [NotNullWhen(true)] out Dictionary<BodyPartSlot, IBodyPart>? dropped)
        {
            DebugTools.AssertNotNull(slot);

            if (!SlotIds.TryGetValue(slot.Id, out var ownedSlot) ||
                ownedSlot != slot ||
                slot.Part == null)
            {
                dropped = null;
                return false;
            }

            var oldPart = slot.Part;
            dropped = GetHangingParts(slot);

            if (!slot.RemovePart())
            {
                dropped = null;
                return false;
            }

            dropped[slot] = oldPart;
            return true;
        }

        public bool ConnectedToCenter(IBodyPart part)
        {
            return TryGetSlot(part, out var result) &&
                   ConnectedToCenterPartRecursion(result);
        }

        private bool ConnectedToCenterPartRecursion(BodyPartSlot slot, HashSet<BodyPartSlot>? searched = null)
        {
            searched ??= new HashSet<BodyPartSlot>();

            if (Template?.CenterSlot == null)
            {
                return false;
            }

            if (slot.Part == CenterPart)
            {
                return true;
            }

            searched.Add(slot);

            foreach (var connection in slot.Connections)
            {
                if (!searched.Contains(connection) &&
                    ConnectedToCenterPartRecursion(connection, searched))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSlot(string slot)
        {
            return SlotIds.ContainsKey(slot);
        }

        public IEnumerable<IBodyPart> GetParts()
        {
            foreach (var slot in SlotIds.Values)
            {
                if (slot.Part != null)
                {
                    yield return slot.Part;
                }
            }
        }

        public bool TryGetPart(string slotId, [NotNullWhen(true)] out IBodyPart? result)
        {
            result = null;

            return SlotIds.TryGetValue(slotId, out var slot) &&
                   (result = slot.Part) != null;
        }

        public BodyPartSlot? GetSlot(string id)
        {
            return SlotIds.GetValueOrDefault(id);
        }

        public BodyPartSlot? GetSlot(IBodyPart part)
        {
            return SlotParts.GetValueOrDefault(part);
        }

        public bool TryGetSlot(string slotId, [NotNullWhen(true)] out BodyPartSlot? slot)
        {
            return (slot = GetSlot(slotId)) != null;
        }

        public bool TryGetSlot(IBodyPart part, [NotNullWhen(true)] out BodyPartSlot? slot)
        {
            return (slot = GetSlot(part)) != null;
        }

        public bool TryGetPartConnections(string slotId, [NotNullWhen(true)] out List<IBodyPart>? connections)
        {
            if (!SlotIds.TryGetValue(slotId, out var slot))
            {
                connections = null;
                return false;
            }

            connections = new List<IBodyPart>();
            foreach (var connection in slot.Connections)
            {
                if (connection.Part != null)
                {
                    connections.Add(connection.Part);
                }
            }

            if (connections.Count <= 0)
            {
                connections = null;
                return false;
            }

            return true;
        }

        public bool HasSlotOfType(BodyPartType type)
        {
            foreach (var _ in GetSlotsOfType(type))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<BodyPartSlot> GetSlotsOfType(BodyPartType type)
        {
            foreach (var slot in SlotIds.Values)
            {
                if (slot.PartType == type)
                {
                    yield return slot;
                }
            }
        }

        public bool HasPartOfType(BodyPartType type)
        {
            foreach (var _ in GetPartsOfType(type))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<IBodyPart> GetPartsOfType(BodyPartType type)
        {
            foreach (var slot in GetSlotsOfType(type))
            {
                if (slot.Part != null)
                {
                    yield return slot.Part;
                }
            }
        }

        public IEnumerable<(IBodyPart part, IBodyPartProperty property)> GetPartsWithProperty(Type type)
        {
            foreach (var slot in SlotIds.Values)
            {
                if (slot.Part != null && slot.Part.TryGetProperty(type, out var property))
                {
                    yield return (slot.Part, property);
                }
            }
        }

        public IEnumerable<(IBodyPart part, T property)> GetPartsWithProperty<T>() where T : class, IBodyPartProperty
        {
            foreach (var part in SlotParts.Keys)
            {
                if (part.TryGetProperty<T>(out var property))
                {
                    yield return (part, property);
                }
            }
        }

        private void CalculateSpeed()
        {
            if (!Owner.TryGetComponent(out MovementSpeedModifierComponent? playerMover))
            {
                return;
            }

            var legs = GetPartsWithProperty<LegComponent>().ToArray();
            float speedSum = 0;

            foreach (var leg in legs)
            {
                var footDistance = DistanceToNearestFoot(leg.part);

                if (Math.Abs(footDistance - float.MinValue) <= 0.001f)
                {
                    continue;
                }

                speedSum += leg.property.Speed * (1 + (float) Math.Log(footDistance, 1024.0));
            }

            if (speedSum <= 0.001f)
            {
                playerMover.BaseWalkSpeed = 0.8f;
                playerMover.BaseSprintSpeed = 2.0f;
            }
            else
            {
                // Extra legs stack diminishingly.
                playerMover.BaseWalkSpeed =
                    speedSum / (legs.Length - (float) Math.Log(legs.Length, 4.0));

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
                CalculateSpeed();
            }

            Dirty();
        }

        /// <summary>
        ///     Returns the combined length of the distance to the nearest
        ///     <see cref="IBodyPart"/> that is a foot.
        ///     If you consider a <see cref="IBody"/> a node map, then it will
        ///     look for a foot node from the given node. It can only search
        ///     through <see cref="IBodyPart"/>s with an
        ///     <see cref="ExtensionComponent"/>.
        /// </summary>
        /// <returns>
        ///     The distance to the foot if found, <see cref="float.MinValue"/>
        ///     otherwise.
        /// </returns>
        public float DistanceToNearestFoot(IBodyPart source)
        {
            if (source.PartType == BodyPartType.Foot &&
                source.TryGetProperty<ExtensionComponent>(out var extension))
            {
                return extension.Distance;
            }

            return LookForFootRecursion(source);
        }

        private float LookForFootRecursion(IBodyPart current, HashSet<BodyPartSlot>? searched = null)
        {
            searched ??= new HashSet<BodyPartSlot>();

            if (!current.TryGetProperty<ExtensionComponent>(out var extProperty))
            {
                return float.MinValue;
            }

            // Get all connected parts if the current part has an extension property
            if (!TryGetSlot(current, out var slot))
            {
                return float.MinValue;
            }

            // If a connected BodyPart is a foot, return this BodyPart's length.
            foreach (var connection in slot.Connections)
            {
                if (connection.PartType == BodyPartType.Foot &&
                    !searched.Contains(connection))
                {
                    return extProperty.Distance;
                }
            }

            // Otherwise, get the recursion values of all connected BodyParts and
            // store them in a list.
            var distances = new List<float>();
            foreach (var connection in slot.Connections)
            {
                if (connection.Part == null || !searched.Contains(connection))
                {
                    continue;
                }

                var result = LookForFootRecursion(connection.Part, searched);

                if (Math.Abs(result - float.MinValue) > 0.001f)
                {
                    distances.Add(result);
                }
            }

            // If one or more of the searches found a foot, return the smallest one
            // and add this ones length.
            if (distances.Count > 0)
            {
                return distances.Min<float>() + extProperty.Distance;
            }

            return float.MinValue;
        }

        // TODO BODY optimize this
        public BodyPartSlot SlotAt(int index)
        {
            return SlotIds.Values.ElementAt(index);
        }

        public KeyValuePair<IBodyPart, BodyPartSlot> PartAt(int index)
        {
            return SlotParts.ElementAt(index);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var parts = new (string slot, EntityUid partId)[SlotParts.Count];

            var i = 0;
            foreach (var (part, slot) in SlotParts)
            {
                parts[i] = (slot.Id, part.Owner.Uid);
                i++;
            }

            return new BodyComponentState(parts);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not BodyComponentState state)
            {
                return;
            }

            var newParts = state.Parts();

            foreach (var (oldPart, slot) in SlotParts)
            {
                if (!newParts.TryGetValue(slot.Id, out var newPart) ||
                    newPart != oldPart)
                {
                    RemovePart(oldPart);
                }
            }

            foreach (var (slotId, newPart) in newParts)
            {
                if (!SlotIds.TryGetValue(slotId, out var slot) ||
                    slot.Part != newPart)
                {
                    SetPart(slotId, newPart);
                }
            }
        }

        public virtual void Gib(bool gibParts = false)
        {
            foreach (var part in SlotParts.Keys)
            {
                RemovePart(part);

                if (gibParts)
                    part.Gib();
            }
        }
    }

    [Serializable, NetSerializable]
    public class BodyComponentState : ComponentState
    {
        private Dictionary<string, IBodyPart>? _parts;

        public readonly (string slot, EntityUid partId)[] PartIds;

        public BodyComponentState((string slot, EntityUid partId)[] partIds) : base(ContentNetIDs.BODY)
        {
            PartIds = partIds;
        }

        public Dictionary<string, IBodyPart> Parts(IEntityManager? entityManager = null)
        {
            if (_parts != null)
            {
                return _parts;
            }

            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var parts = new Dictionary<string, IBodyPart>(PartIds.Length);

            foreach (var (slot, partId) in PartIds)
            {
                if (!entityManager.TryGetEntity(partId, out var entity))
                {
                    continue;
                }

                if (!entity.TryGetComponent(out IBodyPart? part))
                {
                    continue;
                }

                parts[slot] = part;
            }

            return _parts = parts;
        }
    }
}
