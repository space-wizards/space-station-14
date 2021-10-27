using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Behavior;
using Content.Shared.Body.Part;
using Content.Shared.Body.Part.Property;
using Content.Shared.Body.Preset;
using Content.Shared.Body.Slot;
using Content.Shared.Body.Template;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Components
{
    // TODO BODY Damage methods for collections of IDamageableComponents

    [NetworkedComponent()]
    public abstract class SharedBodyComponent : Component, IBodyPartContainer, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Body";

        [ViewVariables]
        [DataField("template", required: true)]
        private string? TemplateId { get; } = default;

        [ViewVariables]
        [DataField("preset", required: true)]
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
        private Dictionary<SharedBodyPartComponent, BodyPartSlot> SlotParts { get; } = new();

        [ViewVariables]
        public IEnumerable<BodyPartSlot> Slots => SlotIds.Values;

        [ViewVariables]
        public IEnumerable<KeyValuePair<SharedBodyPartComponent, BodyPartSlot>> Parts => SlotParts;

        [ViewVariables]
        public IEnumerable<BodyPartSlot> EmptySlots => Slots.Where(slot => slot.Part == null);

        public BodyPartSlot? CenterSlot =>
            Template?.CenterSlot is { } centerSlot
                ? SlotIds.GetValueOrDefault(centerSlot)
                : null;

        public SharedBodyPartComponent? CenterPart => CenterSlot?.Part;

        protected override void Initialize()
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
        }

        protected override void OnRemove()
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

        private Dictionary<BodyPartSlot, SharedBodyPartComponent> GetHangingParts(BodyPartSlot from)
        {
            var hanging = new Dictionary<BodyPartSlot, SharedBodyPartComponent>();

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

        protected virtual bool CanAddPart(string slotId, SharedBodyPartComponent part)
        {
            if (!SlotIds.TryGetValue(slotId, out var slot) ||
                slot.CanAddPart(part))
            {
                return false;
            }

            return true;
        }

        protected virtual void OnAddPart(BodyPartSlot slot, SharedBodyPartComponent part)
        {
            SlotParts[part] = slot;
            part.Body = this;

            var argsAdded = new BodyPartAddedEventArgs(slot.Id, part);

            EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner.Uid, argsAdded);
            foreach (var component in Owner.GetAllComponents<IBodyPartAdded>().ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }

            // TODO BODY Sort this duplicate out
            OnBodyChanged();
        }

        protected virtual void OnRemovePart(BodyPartSlot slot, SharedBodyPartComponent part)
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


            EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartRemoved(Owner.Uid, args);
            foreach (var component in Owner.GetAllComponents<IBodyPartRemoved>())
            {
                component.BodyPartRemoved(args);
            }

            // creadth: fall down if no legs
            if (part.PartType == BodyPartType.Leg &&
                GetPartsOfType(BodyPartType.Leg).ToArray().Length == 0)
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner.Uid);
            }

            if (part.IsVital && SlotParts.Count(x => x.Value.PartType == part.PartType) == 0)
            {
                // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
                var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), 300);
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(part.Owner.Uid, damage);
            }

            OnBodyChanged();
        }

        // TODO BODY Sensible templates
        public bool TryAddPart(string slotId, SharedBodyPartComponent part)
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

        public void SetPart(string slotId, SharedBodyPartComponent part)
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

        public bool HasPart(SharedBodyPartComponent part)
        {
            DebugTools.AssertNotNull(part);

            return SlotParts.ContainsKey(part);
        }

        public bool RemovePart(SharedBodyPartComponent part)
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

        public bool RemovePart(SharedBodyPartComponent part, [NotNullWhen(true)] out BodyPartSlot? slotId)
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

        public bool TryDropPart(BodyPartSlot slot, [NotNullWhen(true)] out Dictionary<BodyPartSlot, SharedBodyPartComponent>? dropped)
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

        public bool ConnectedToCenter(SharedBodyPartComponent part)
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

        public IEnumerable<SharedBodyPartComponent> GetParts()
        {
            foreach (var slot in SlotIds.Values)
            {
                if (slot.Part != null)
                {
                    yield return slot.Part;
                }
            }
        }

        public bool TryGetPart(string slotId, [NotNullWhen(true)] out SharedBodyPartComponent? result)
        {
            result = null;

            return SlotIds.TryGetValue(slotId, out var slot) &&
                   (result = slot.Part) != null;
        }

        public BodyPartSlot? GetSlot(string id)
        {
            return SlotIds.GetValueOrDefault(id);
        }

        public BodyPartSlot? GetSlot(SharedBodyPartComponent part)
        {
            return SlotParts.GetValueOrDefault(part);
        }

        public bool TryGetSlot(string slotId, [NotNullWhen(true)] out BodyPartSlot? slot)
        {
            return (slot = GetSlot(slotId)) != null;
        }

        public bool TryGetSlot(SharedBodyPartComponent part, [NotNullWhen(true)] out BodyPartSlot? slot)
        {
            return (slot = GetSlot(part)) != null;
        }

        public bool TryGetPartConnections(string slotId, [NotNullWhen(true)] out List<SharedBodyPartComponent>? connections)
        {
            if (!SlotIds.TryGetValue(slotId, out var slot))
            {
                connections = null;
                return false;
            }

            connections = new List<SharedBodyPartComponent>();
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

        public IEnumerable<SharedBodyPartComponent> GetPartsOfType(BodyPartType type)
        {
            foreach (var slot in GetSlotsOfType(type))
            {
                if (slot.Part != null)
                {
                    yield return slot.Part;
                }
            }
        }

        /// <returns>A list of parts with that property.</returns>
        public IEnumerable<(SharedBodyPartComponent part, IBodyPartProperty property)> GetPartsWithProperty(Type type)
        {
            foreach (var slot in SlotIds.Values)
            {
                if (slot.Part != null && slot.Part.TryGetProperty(type, out var property))
                {
                    yield return (slot.Part, property);
                }
            }
        }

        public IEnumerable<(SharedBodyPartComponent part, T property)> GetPartsWithProperty<T>() where T : class, IBodyPartProperty
        {
            foreach (var part in SlotParts.Keys)
            {
                if (part.TryGetProperty<T>(out var property))
                {
                    yield return (part, property);
                }
            }
        }


        private void OnBodyChanged()
        {
            Dirty();
        }

        public float DistanceToNearestFoot(SharedBodyPartComponent source)
        {
            if (source.PartType == BodyPartType.Foot &&
                source.TryGetProperty<ExtensionComponent>(out var extension))
            {
                return extension.Distance;
            }

            return LookForFootRecursion(source);
        }

        private float LookForFootRecursion(SharedBodyPartComponent current, HashSet<BodyPartSlot>? searched = null)
        {
            searched ??= new HashSet<BodyPartSlot>();

            if (!current.TryGetProperty<ExtensionComponent>(out var extProperty))
            {
                return float.MinValue;
            }

            if (!TryGetSlot(current, out var slot))
            {
                return float.MinValue;
            }

            foreach (var connection in slot.Connections)
            {
                if (connection.PartType == BodyPartType.Foot &&
                    !searched.Contains(connection))
                {
                    return extProperty.Distance;
                }
            }

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

        public KeyValuePair<SharedBodyPartComponent, BodyPartSlot> PartAt(int index)
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

        public bool TryGetMechanismBehaviors([NotNullWhen(true)] out List<SharedMechanismBehavior>? behaviors)
        {
            behaviors = GetMechanismBehaviors().ToList();

            if (behaviors.Count == 0)
            {
                behaviors = null;
                return false;
            }

            return true;
        }

        public bool HasMechanismBehavior<T>() where T : SharedMechanismBehavior
        {
            return Parts.Any(p => p.Key.HasMechanismBehavior<T>());
        }

        // TODO cache these 2 methods jesus
        public IEnumerable<SharedMechanismBehavior> GetMechanismBehaviors()
        {
            foreach (var (part, _) in Parts)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Behaviors.Values)
            {
                yield return behavior;
            }
        }

        public IEnumerable<T> GetMechanismBehaviors<T>() where T : SharedMechanismBehavior
        {
            foreach (var (part, _) in Parts)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Behaviors.Values)
            {
                if (behavior is T tBehavior)
                {
                    yield return tBehavior;
                }
            }
        }

        public bool TryGetMechanismBehaviors<T>([NotNullWhen(true)] out List<T>? behaviors)
            where T : SharedMechanismBehavior
        {
            behaviors = GetMechanismBehaviors<T>().ToList();

            if (behaviors.Count == 0)
            {
                behaviors = null;
                return false;
            }

            return true;
        }
    }

    [Serializable, NetSerializable]
    public class BodyComponentState : ComponentState
    {
        private Dictionary<string, SharedBodyPartComponent>? _parts;

        public readonly (string slot, EntityUid partId)[] PartIds;

        public BodyComponentState((string slot, EntityUid partId)[] partIds)
        {
            PartIds = partIds;
        }

        public Dictionary<string, SharedBodyPartComponent> Parts(IEntityManager? entityManager = null)
        {
            if (_parts != null)
            {
                return _parts;
            }

            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var parts = new Dictionary<string, SharedBodyPartComponent>(PartIds.Length);

            foreach (var (slot, partId) in PartIds)
            {
                if (!entityManager.TryGetEntity(partId, out var entity))
                {
                    continue;
                }

                if (!entity.TryGetComponent(out SharedBodyPartComponent? part))
                {
                    continue;
                }

                parts[slot] = part;
            }

            return _parts = parts;
        }
    }
}
