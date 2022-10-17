using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
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
        SubscribeLocalEvent<BodyPartComponent, ComponentRemove>(OnPartRemoved);

        InitializeStateHandling();
    }

    private void OnBodyMapInit(EntityUid bodyId, BodyComponent body, MapInitEvent args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (body.Prototype == null || body.Root != null)
            return;

        var prototype = _prototypes.Index<BodyPrototype>(body.Prototype);
        InitBody(body, prototype);
    }

    private void OnBodyInit(EntityUid bodyId, BodyComponent body, ComponentInit args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (body.Prototype == null || body.Root != null)
            return;

        var prototype = _prototypes.Index<BodyPrototype>(body.Prototype);
        InitBody(body, prototype);
    }

    private void InitBody(BodyComponent body, BodyPrototype prototype)
    {
        var root = prototype.Slots[prototype.Root];
        var bodyId = Spawn(root.Part, body.Owner.ToCoordinates());
        var partComponent = Comp<BodyPartComponent>(bodyId);
        var slot = new BodyPartSlot(root.Part, body.Owner, partComponent.PartType);
        body.Root = slot;

        _containers.EnsureContainer<Container>(body.Owner, BodyContainerId);

        AttachPart(bodyId, slot, partComponent);
        InitPart(partComponent, prototype, prototype.Root);
    }

    private void InitPart(BodyPartComponent parent, BodyPrototype prototype, string slotId, HashSet<string>? initialized = null)
    {
        initialized ??= new HashSet<string>();

        if (initialized.Contains(slotId))
            return;

        initialized.Add(slotId);

        var (_, connections, organs) = prototype.Slots[slotId];
        connections = new HashSet<string>(connections);
        connections.ExceptWith(initialized);

        var coordinates = parent.Owner.ToCoordinates();
        var subConnections = new List<(BodyPartComponent child, string slotId)>();

        _containers.EnsureContainer<Container>(parent.Owner, BodyContainerId);

        foreach (var connection in connections)
        {
            var childSlot = prototype.Slots[connection];
            var childPart = Spawn(childSlot.Part, coordinates);
            var childPartComponent = Comp<BodyPartComponent>(childPart);
            var slot = CreatePartSlot(connection, parent.Owner, childPartComponent.PartType, parent);
            if (slot == null)
            {
                Logger.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                continue;
            }

            AttachPart(childPart, slot, childPartComponent);
            subConnections.Add((childPartComponent, connection));
        }

        foreach (var (organSlotId, organId) in organs)
        {
            var organ = Spawn(organId, coordinates);
            var organComponent = Comp<OrganComponent>(organ);

            var slot = CreateOrganSlot(organSlotId, parent.Owner, parent);
            if (slot == null)
            {
                Logger.Error($"Could not create slot for connection {organSlotId} in body {prototype.ID}");
                continue;
            }

            InsertOrgan(organ, slot, organComponent);
        }

        foreach (var connection in subConnections)
        {
            InitPart(connection.child, prototype, connection.slotId, initialized);
        }
    }

    private BodyPartSlot? CreatePartSlot(
        string slotId,
        EntityUid parent,
        BodyPartType partType,
        BodyPartComponent? part = null)
    {
        if (!Resolve(parent, ref part, false))
            return null;

        var slot = new BodyPartSlot(slotId, parent, partType);
        part.Children.Add(slotId, slot);

        return slot;
    }

    private OrganSlot? CreateOrganSlot(string slotId, EntityUid parent, BodyPartComponent? part = null)
    {
        if (!Resolve(parent, ref part, false))
            return null;

        var slot = new OrganSlot(slotId, parent);
        part.Organs.Add(slotId, slot);

        return slot;
    }

    private void OnPartRemoved(EntityUid uid, BodyPartComponent part, ComponentRemove args)
    {
        if (part.ParentSlot is { } slot)
        {
            slot.Child = null;
            Dirty(slot.Parent);
        }

        foreach (var childSlot in part.Children.Values.ToArray())
        {
            DropPart(childSlot.Child);
        }
    }

    public bool TryCreatePartSlot(
        EntityUid? parentId,
        string id,
        [NotNullWhen(true)] out BodyPartSlot? slot,
        BodyPartComponent? parent = null)
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

    public bool TryCreatePartSlotAndAttach(
        EntityUid? parentId,
        string id,
        EntityUid? childId,
        BodyPartComponent? parent = null,
        BodyPartComponent? child = null)
    {
        return TryCreatePartSlot(parentId, id, out var slot, parent) && AttachPart(childId, slot, child);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid? id, BodyComponent? body = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref body, false) ||
            !TryComp(body.Root.Child, out BodyPartComponent? part))
            yield break;

        yield return (body.Root.Child.Value, part);

        foreach (var child in GetPartChildren(body.Root.Child))
        {
            yield return child;
        }
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetPartChildren(EntityUid? id, BodyPartComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            yield break;

        foreach (var slot in part.Children.Values)
        {
            if (!TryComp(slot.Child, out BodyPartComponent? childPart))
                continue;

            yield return (slot.Child.Value, childPart);

            foreach (var subChild in GetPartChildren(slot.Child, childPart))
            {
                yield return subChild;
            }
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetBodyOrgans(EntityUid? bodyId, BodyComponent? body = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            yield break;

        foreach (var part in GetBodyChildren(bodyId, body))
        {
            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                yield return organ;
            }
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetPartOrgans(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false))
            yield break;

        foreach (var slot in part.Organs.Values)
        {
            if (!TryComp(slot.Child, out OrganComponent? organ))
                continue;

            yield return (slot.Child.Value, organ);
        }
    }

    public IEnumerable<BodyPartSlot> GetBodySlots(EntityUid? bodyId, BodyComponent? body = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            yield break;

        foreach (var slot in GetPartSlots(body.Root.Child))
        {
            yield return slot;
        }
    }
    public IEnumerable<BodyPartSlot> GetPartSlots(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false))
            yield break;

        foreach (var slot in part.Children.Values)
        {
            yield return slot;

            if (!TryComp(slot.Child, out BodyComponent? childPart))
                continue;

            foreach (var subChild in GetBodySlots(slot.Child, childPart))
            {
                yield return subChild;
            }
        }
    }

    public (EntityUid Id, BodyComponent Component)? GetPartBody(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            part.ParentSlot?.Parent is not { } parent)
            return null;

        if (TryComp(parent, out BodyComponent? body))
            return (parent, body);

        return GetPartBody(parent);
    }

    public bool TryGetPartBody(
        EntityUid? partId,
        [NotNullWhen(true)] out (EntityUid Id, BodyComponent Component)? body,
        BodyPartComponent? part = null)
    {
        return (body = GetPartBody(partId, part)) != null;
    }

    public (EntityUid Id, BodyComponent Component)? GetOrganBody(EntityUid? id, OrganComponent? organ = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref organ, false) ||
            organ.ParentSlot?.Parent is not { } parent)
            return null;

        if (TryComp(parent, out BodyComponent? body))
            return (parent, body);

        return GetPartBody(parent);
    }

    public bool TryGetOrganBody(
        EntityUid? organId,
        [NotNullWhen(true)] out (EntityUid Id, BodyComponent Component)? body,
        OrganComponent? organ = null)
    {
        return (body = GetOrganBody(organId, organ)) != null;
    }

    public virtual HashSet<EntityUid> Gib(EntityUid? partId, bool gibOrgans = false,
        BodyComponent? body = null)
    {
        if (partId == null || !Resolve(partId.Value, ref body, false))
            return new HashSet<EntityUid>();

        var parts = GetBodyChildren(partId, body).ToArray();
        var gibs = new HashSet<EntityUid>(parts.Length);

        foreach (var part in parts)
        {
            DropPart(part.Id, part.Component);
            gibs.Add(part.Id);

            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                DropOrgan(organ.Id, organ.Component);
                gibs.Add(organ.Id);
            }
        }

        return gibs;
    }

    public bool CanAttachPart([NotNullWhen(true)] EntityUid? partId, BodyPartSlot slot, BodyPartComponent? part = null)
    {
        return partId != null &&
               slot.Child == null &&
               Resolve(partId.Value, ref part, false) &&
               (slot.Type == null || slot.Type == part.PartType) &&
               _containers.TryGetContainer(slot.Parent, BodyContainerId, out var container) &&
               container.CanInsert(partId.Value);
    }

    public virtual bool AttachPart(
        EntityUid? partId,
        BodyPartSlot slot,
        [NotNullWhen(true)] BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            !CanAttachPart(partId, slot, part))
            return false;

        DropPart(slot.Child);
        DropPart(partId, part);

        var container = _containers.EnsureContainer<Container>(slot.Parent, BodyContainerId);
        if (!container.Insert(partId.Value))
            return false;

        slot.Child = partId;
        part.ParentSlot = slot;

        Dirty(slot.Parent);
        Dirty(partId.Value);

        if (TryGetPartBody(partId, out var body, part))
        {
            var argsAdded = new BodyPartAddedEventArgs(slot.Id, part);

            // TODO: Body refactor. Somebody is doing it
            // EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner, argsAdded);
            foreach (var component in AllComps<IBodyPartAdded>(body.Value.Id).ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }

            foreach (var organ in GetPartOrgans(partId, part))
            {
                RaiseLocalEvent(organ.Id, new AddedToBodyEvent(body.Value.Component), true);
            }

            Dirty(body.Value.Id);
        }

        return true;
    }

    private bool CanInsertOrgan(EntityUid? organId, OrganSlot slot, OrganComponent? organ = null)
    {
        return organId != null &&
               slot.Child == null &&
               Resolve(organId.Value, ref organ, false) &&
               _containers.TryGetContainer(slot.Parent, BodyContainerId, out var container) &&
               container.CanInsert(organId.Value);
    }

    public bool InsertOrgan(EntityUid? organId, OrganSlot slot, OrganComponent? organ = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) ||
            !CanInsertOrgan(organId, slot, organ))
            return false;

        DropOrgan(slot.Child);
        DropOrgan(organId, organ);

        var container = _containers.EnsureContainer<Container>(slot.Parent, BodyContainerId);
        if (!container.Insert(organId.Value))
            return false;

        slot.Child = organId;
        organ.ParentSlot = slot;

        Dirty(slot.Parent);
        Dirty(organId.Value);

        if (TryComp(slot.Parent, out BodyPartComponent? part))
        {
            if (TryGetOrganBody(organId, out var body, organ))
            {
                RaiseLocalEvent(organId.Value, new AddedToPartInBodyEvent(body.Value.Component, part));
            }
            else
            {
                RaiseLocalEvent(organId.Value, new AddedToPartEvent(part));
            }
        }

        return true;
    }

    public bool AddOrganToFirstValidSlot(
        EntityUid? childId,
        EntityUid? parentId,
        OrganComponent? child = null,
        BodyPartComponent? parent = null)
    {
        if (childId == null ||
            !Resolve(childId.Value, ref child, false) ||
            parentId == null ||
            !Resolve(parentId.Value, ref parent, false))
            return false;

        foreach (var slot in parent.Organs.Values)
        {
            if (slot.Child == null)
                continue;

            InsertOrgan(childId, slot, child);
            return true;
        }

        return false;
    }

    public virtual bool DropPart(EntityUid? partId, [NotNullWhen(true)] BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            part.ParentSlot is not { } slot)
            return false;

        var root = GetPartBody(partId, part);

        slot.Child = null;
        part.ParentSlot = null;

        if (_containers.TryGetContainer(slot.Parent, BodyContainerId, out var container))
            container.Remove(partId.Value);

        if (TryComp(partId, out TransformComponent? transform))
            transform.AttachToGridOrMap();

        part.Owner.RandomOffset(0.25f);

        if (root != null)
        {
            var (bodyId, bodyComponent) = root.Value;
            var args = new BodyPartRemovedEventArgs(slot.Id, part);
            foreach (var component in AllComps<IBodyPartRemoved>(bodyId))
            {
                component.BodyPartRemoved(args);
            }

            if (part.PartType == BodyPartType.Leg &&
                !GetBodyChildrenOfType(bodyId, BodyPartType.Leg, bodyComponent).Any())
            {
                _standing.Down(bodyId);
            }

            if (part.IsVital && !GetBodyChildrenOfType(bodyId, part.PartType, bodyComponent).Any())
            {
                // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
                var damage = new DamageSpecifier(_prototypes.Index<DamageTypePrototype>("Bloodloss"), 300);
                _damageable.TryChangeDamage(part.Owner, damage);
            }

            foreach (var organSlot in part.Organs.Values)
            {
                if (organSlot.Child is not { } child)
                    continue;

                RaiseLocalEvent(child, new RemovedFromBodyEvent(bodyComponent), true);
            }
        }

        Dirty(slot.Parent);
        Dirty(partId.Value);

        return true;
    }

    public bool DropOrgan(EntityUid? organId, OrganComponent? organ = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) ||
            organ.ParentSlot is not { } slot)
            return false;

        var oldParent = CompOrNull<BodyPartComponent>(organ.ParentSlot.Parent);

        slot.Child = null;
        organ.ParentSlot = null;

        if (_containers.TryGetContainer(slot.Parent, BodyContainerId, out var container))
            container.Remove(organId.Value);

        if (TryComp(organId, out TransformComponent? transform))
            transform.AttachToGridOrMap();

        organ.Owner.RandomOffset(0.25f);

        if (oldParent != null)
        {
            if (TryGetOrganBody(organId, out var body, organ))
            {
                RaiseLocalEvent(organId.Value, new RemovedFromPartInBodyEvent(body.Value.Component, oldParent));
            }
            else
            {
                RaiseLocalEvent(organId.Value, new RemovedFromPartEvent(oldParent));
            }
        }

        return true;
    }

    public bool DropPartAt(EntityUid? partId, EntityCoordinates dropAt, BodyPartComponent? part = null)
    {
        if (partId == null || !DropPart(partId, part))
            return false;

        if (TryComp(partId.Value, out TransformComponent? transform))
            transform.Coordinates = dropAt;

        return true;
    }

    public bool DropOrganAt(EntityUid? organId, EntityCoordinates dropAt, OrganComponent? organ = null)
    {
        if (organId == null || !DropOrgan(organId, organ))
            return false;

        if (TryComp(organId.Value, out TransformComponent? transform))
            transform.Coordinates = dropAt;

        return true;
    }

    public bool OrphanPart(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false))
            return false;

        DropPart(partId, part);

        foreach (var slot in part.Children.Values)
        {
            DropPart(slot.Child);
        }

        return false;
    }

    public bool DeletePart(EntityUid? id, BodyPartComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            return false;

        DropPart(id, part);

        if (Deleted(id.Value))
            return false;

        Del(id.Value);
        return true;
    }

    public bool DeleteOrgan(EntityUid? id, OrganComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            return false;

        DropOrgan(id, part);

        if (Deleted(id.Value))
            return false;

        Del(id.Value);
        return true;
    }
}
