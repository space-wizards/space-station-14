using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Components
{
    // TODO BODY Damage methods for collections of IDamageableComponents

    [NetworkedComponent()]
    public abstract class SharedBodyComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Body";

        [ViewVariables]
        [DataField("template", required: true)]
        private string? TemplateId { get; }

        [ViewVariables]
        [DataField("preset", required: true)]
        private string? PresetId { get; }

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

            EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner, argsAdded);
            foreach (var component in IoCManager.Resolve<IEntityManager>().GetComponents<IBodyPartAdded>(Owner).ToArray())
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


            EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartRemoved(Owner, args);
            foreach (var component in IoCManager.Resolve<IEntityManager>().GetComponents<IBodyPartRemoved>(Owner))
            {
                component.BodyPartRemoved(args);
            }

            // creadth: fall down if no legs
            if (part.PartType == BodyPartType.Leg &&
                GetPartsOfType(BodyPartType.Leg).ToArray().Length == 0)
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner);
            }

            if (part.IsVital && SlotParts.Count(x => x.Value.PartType == part.PartType) == 0)
            {
                // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
                var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), 300);
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(part.Owner, damage);
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

        public BodyPartSlot? GetSlot(SharedBodyPartComponent part)
        {
            return SlotParts.GetValueOrDefault(part);
        }

        public bool TryGetSlot(SharedBodyPartComponent part, [NotNullWhen(true)] out BodyPartSlot? slot)
        {
            return (slot = GetSlot(part)) != null;
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

        private void OnBodyChanged()
        {
            Dirty();
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

        public override ComponentState GetComponentState()
        {
            var parts = new (string slot, EntityUid partId)[SlotParts.Count];

            var i = 0;
            foreach (var (part, slot) in SlotParts)
            {
                parts[i] = (slot.Id, part.Owner);
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
                if (!entityManager.EntityExists(partId))
                {
                    continue;
                }

                if (!entityManager.TryGetComponent(partId, out SharedBodyPartComponent? part))
                {
                    continue;
                }

                parts[slot] = part;
            }

            return _parts = parts;
        }
    }
}
