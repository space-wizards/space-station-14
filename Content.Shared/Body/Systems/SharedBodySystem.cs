using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems;

[Virtual]
public abstract partial class SharedBodySystem : EntitySystem
{
    private const string BodyContainerId = "BodyContainer";

    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<BodyComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<BodyComponent, ComponentRemove>(OnPartRemoved);
        SubscribeLocalEvent<BodyComponent, ComponentGetState>(OnPartGetState);
        SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnPartHandleState);
    }

    private void OnBodyMapInit(EntityUid bodyId, BodyComponent body, MapInitEvent args)
    {
        if (body.Prototype == null || body.Children.Count > 0)
            return;

        var prototype = _prototypes.Index<BodyPrototype>(body.Prototype);
        InitBody(body, prototype);
    }

    private void OnBodyInit(EntityUid bodyId, BodyComponent body, ComponentInit args)
    {
        if (body.Prototype == null || body.Children.Count > 0)
            return;

        var prototype = _prototypes.Index<BodyPrototype>(body.Prototype);
        InitBody(body, prototype);
    }

    private void InitBody(BodyComponent body, BodyPrototype prototype)
    {
        var root = prototype.Slots[prototype.Root];
        var partId = Spawn(root.Part, body.Owner.ToCoordinates());
        var partComponent = Comp<BodyComponent>(partId);
        var slot = CreateSlot(root.Part, body, partComponent.PartType);

        _containers.EnsureContainer<Container>(body.Owner, BodyContainerId);

        Attach(partId, slot, partComponent);
        InitPart(partComponent, prototype, prototype.Root);
    }

    private void InitPart(BodyComponent parent, BodyPrototype prototype, string slotId)
    {
        var (_, connections, organs) = prototype.Slots[slotId];
        var coordinates = parent.Owner.ToCoordinates();
        var subConnections = new List<(BodyComponent child, string slotId)>();

        _containers.EnsureContainer<Container>(parent.Owner, BodyContainerId);

        foreach (var connection in connections)
        {
            var childSlot = prototype.Slots[connection];
            var childPart = Spawn(childSlot.Part, coordinates);
            var childPartComponent = Comp<BodyComponent>(childPart);
            var slot = CreateSlot(connection, parent, childPartComponent.PartType);

            Attach(childPart, slot, childPartComponent);
            subConnections.Add((childPartComponent, connection));
        }

        foreach (var (organSlotId, organId) in organs)
        {
            var organ = Spawn(organId, coordinates);
            var organComponent = Comp<BodyComponent>(organ);
            // TODO BODY BEFORE MERGE move to prototype, and make all bodies non attachable and non organs
            organComponent.Attachable = false;
            organComponent.Organ = true;

            var slot = CreateSlot(organSlotId, parent, organComponent.PartType);

            Attach(organ, slot, organComponent);
        }

        foreach (var connection in subConnections)
        {
            InitPart(connection.child, prototype, connection.slotId);
        }
    }

    private BodyPartSlot CreateSlot(string slotId, BodyComponent parent, BodyPartType partType)
    {
        var slot = new BodyPartSlot(slotId, parent.Owner, partType);
        parent.Children.Add(slotId, slot);
        return slot;
    }

    private void OnPartRemoved(EntityUid uid, BodyComponent part, ComponentRemove args)
    {
        if (part.ParentSlot is { } slot)
        {
            slot.Child = null;
            Dirty(slot.Parent);
        }

        foreach (var childSlot in part.Children.Values.ToArray())
        {
            Drop(childSlot.Child);
        }
    }

    private void OnPartGetState(EntityUid uid, BodyComponent part, ref ComponentGetState args)
    {
        args.State = new BodyPartComponentState(
            part.ParentSlot,
            part.Children,
            part.PartType,
            part.IsVital,
            part.Symmetry,
            part.Attachable,
            part.Organ,
            part.GibSound
        );
    }

    private void OnPartHandleState(EntityUid uid, BodyComponent part, ref ComponentHandleState args)
    {
        if (args.Current is not BodyPartComponentState state)
            return;

        part.ParentSlot = state.ParentSlot;
        part.Children = state.Children;
        part.PartType = state.PartType;
        part.IsVital = state.IsVital;
        part.Symmetry = state.Symmetry;
        part.Attachable = state.Attachable;
        part.Organ = state.Organ;
        part.GibSound = state.GibSound;
    }

    public bool TryCreateSlot(
        EntityUid? parentId,
        string id,
        [NotNullWhen(true)] out BodyPartSlot? slot,
        BodyComponent? parent = null)
    {
        slot = null;

        if (parentId == null ||
            !Resolve(parentId.Value, ref parent, false))
            return false;

        slot = new BodyPartSlot(id, parentId.Value, null);
        if (parent.Children.TryAdd(id, slot))
        {
            slot = null;
            return false;
        }

        return true;
    }

    public bool TryCreateSlotAndAttach(
        EntityUid? parentId,
        string id,
        EntityUid? childId,
        BodyComponent? parent = null,
        BodyComponent? child = null)
    {
        return TryCreateSlot(parentId, id, out var slot, parent) && Attach(childId, slot, child);
    }

    public IEnumerable<BodyComponent> GetChildren(EntityUid? id, BodyComponent? part = null,
        bool parts = true, bool organs = true)
    {
        if (id == null ||
            !Resolve(id.Value, ref part, false))
            yield break;

        foreach (var slot in part.Children.Values)
        {
            if (!TryComp(slot.Child, out BodyComponent? childPart))
                continue;

            yield return childPart;

            foreach (var subChild in GetChildren(slot.Child, childPart))
            {
                yield return subChild;
            }
        }
    }

    public IEnumerable<EntityUid> GetChildIds(EntityUid? id, BodyComponent? part = null, bool parts = true,
        bool organs = true)
    {
        foreach (var childPart in GetChildren(id, part, organs, parts))
        {
            switch (childPart.Organ)
            {
                case true when !organs:
                case false when !parts:
                    continue;
                default:
                    yield return childPart.Owner;
                    break;
            }
        }
    }

    public IEnumerable<BodyComponent> GetChildParts(EntityUid? id, BodyComponent? part = null)
    {
        return GetChildren(id, part, organs: false);
    }

    public IEnumerable<BodyComponent> GetChildOrgans(EntityUid? id, BodyComponent? part = null)
    {
        return GetChildren(id, part, parts: false);
    }

    public IEnumerable<BodyPartSlot> GetSlots(EntityUid? id, BodyComponent? part = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref part, false))
            yield break;

        foreach (var slot in part.Children.Values)
        {
            yield return slot;

            if (!TryComp(slot.Child, out BodyComponent? childPart))
                continue;

            foreach (var subChild in GetSlots(slot.Child, childPart))
            {
                yield return subChild;
            }
        }
    }

    public BodyComponent? GetRoot(EntityUid? id, BodyComponent? part = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref part, false))
            return null;

        if (part.ParentSlot is not {Parent: var parent})
            return part;

        return GetRoot(parent);
    }

    public bool TryGetRoot(
        EntityUid? id,
        [NotNullWhen(true)] out BodyComponent? body,
        BodyComponent? part = null)
    {
        return (body = GetRoot(id, part)) != null;
    }

    public virtual HashSet<EntityUid> Gib(EntityUid? partId, bool gibOrgans = false,
        BodyComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false))
            return new HashSet<EntityUid>();

        var gibs = (gibOrgans ? GetChildIds(partId, part) : GetChildIds(partId, part, organs: false)).ToHashSet();

        foreach (var slot in part.Children.Values.ToArray())
        {
            if (!TryComp(slot.Child, out BodyComponent? childPart))
                continue;

            if (childPart.Organ && !gibOrgans)
                continue;

            Drop(slot.Child, childPart);
        }

        return gibs;
    }

    public bool CanAttach([NotNullWhen(true)] EntityUid? partId, BodyPartSlot slot, BodyComponent? part)
    {
        return partId != null &&
               slot.Child == null &&
               Resolve(partId.Value, ref part, false) &&
               (slot.Type == null || slot.Type == part.PartType) &&
               _containers.TryGetContainer(slot.Parent, BodyContainerId, out var container) &&
               container.CanInsert(partId.Value);
    }

    public virtual bool Attach(EntityUid? partId, BodyPartSlot slot,
        [NotNullWhen(true)] BodyComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            !CanAttach(partId, slot, part))
            return false;

        Drop(slot.Child);
        Drop(partId, part);

        var container = _containers.EnsureContainer<Container>(slot.Parent, BodyContainerId);
        if (!container.Insert(partId.Value))
            return false;

        slot.Child = partId;
        part.ParentSlot = slot;

        Dirty(slot.Parent);
        Dirty(partId.Value);

        if (part.Organ)
        {
            if (TryGetRoot(partId, out var body, part))
            {
                RaiseLocalEvent(partId.Value, new AddedToPartInBodyEvent(body, part));
            }
            else
            {
                RaiseLocalEvent(partId.Value, new AddedToPartEvent(part));
            }
        }
        else
        {
            if (TryGetRoot(partId, out var body, part))
            {
                var argsAdded = new BodyPartAddedEventArgs(slot.Id, part);

                // TODO: Body refactor. Somebody is doing it
                // EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner, argsAdded);
                foreach (var component in AllComps<IBodyPartAdded>(body.Owner).ToArray())
                {
                    component.BodyPartAdded(argsAdded);
                }

                foreach (var childSlot in part.Children.Values)
                {
                    if (TryComp(childSlot.Child, out BodyComponent? childPart) &&
                        childPart.Organ)
                        RaiseLocalEvent(slot.Child.Value, new AddedToBodyEvent(body), true);
                }

                Dirty(body);
            }
        }

        return true;
    }

    public bool AddToFirstValidSlot(
        EntityUid? childId,
        EntityUid? parentId,
        BodyComponent? child = null,
        BodyComponent? parent = null)
    {
        if (childId == null ||
            !Resolve(childId.Value, ref child, false) ||
            parentId == null ||
            !Resolve(parentId.Value, ref parent, false))
            return false;

        foreach (var slot in parent.Children.Values)
        {
            if (slot.Child == null || slot.Type != child.PartType)
                continue;

            Attach(childId, slot, child);
            return true;
        }

        return false;
    }

    public virtual bool Drop(EntityUid? partId, BodyComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            part.ParentSlot is not { } slot)
            return false;

        var root = GetRoot(partId, part);

        slot.Child = null;
        part.ParentSlot = null;

        if (_containers.TryGetContainer(slot.Parent, BodyContainerId, out var container))
            container.Remove(partId.Value);

        if (TryComp(partId, out TransformComponent? transform))
            transform.AttachToGridOrMap();

        part.Owner.RandomOffset(0.25f);

        if (part.Organ)
        {
            if (root != null)
            {
                RaiseLocalEvent(partId.Value, new RemovedFromPartInBodyEvent(root, part));
            }
            else
            {
                RaiseLocalEvent(partId.Value, new RemovedFromPartEvent(part));
            }
        }
        else
        {
            if (root != null)
            {
                var args = new BodyPartRemovedEventArgs(slot.Id, part);
                foreach (var component in AllComps<IBodyPartRemoved>(root.Owner))
                {
                    component.BodyPartRemoved(args);
                }

                if (part.PartType == BodyPartType.Leg &&
                    !GetChildrenOfType(root.Owner, BodyPartType.Leg, root).Any())
                {
                    _standing.Down(root.Owner);
                }

                if (part.IsVital && !GetChildrenOfType(root.Owner, part.PartType, root).Any())
                {
                    // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
                    var damage = new DamageSpecifier(_prototypes.Index<DamageTypePrototype>("Bloodloss"), 300);
                    _damageable.TryChangeDamage(part.Owner, damage);
                }

                foreach (var childSlot in part.Children.Values)
                {
                    if (childSlot is {Child: { } child} &&
                        CompOrNull<BodyComponent>(child) is {Organ: true})
                        RaiseLocalEvent(child, new RemovedFromBodyEvent(root), true);
                }
            }
        }

        Dirty(slot.Parent);
        Dirty(partId.Value);

        return true;
    }

    public bool DropAt(EntityUid? partId, EntityCoordinates dropAt, BodyComponent? part = null)
    {
        if (partId == null || !Drop(partId, part))
            return false;

        if (TryComp(partId.Value, out TransformComponent? transform))
            transform.Coordinates = dropAt;

        return true;
    }

    public bool Delete(EntityUid? id, BodyComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            return false;

        Drop(id, part);

        if (Deleted(id.Value))
            return false;

        Del(id.Value);
        return true;
    }
}
