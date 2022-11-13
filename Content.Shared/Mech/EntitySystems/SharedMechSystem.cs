using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<MechComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var v = new AlternativeVerb
            {
                Act = () => TryInsert(uid, args.User, component),
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(v);
        }
        else if (!IsEmpty(component))
        {
            var v = new AlternativeVerb
            {
                Act = () => TryEject(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant"),
                Priority = 1 // Promote to top to make ejecting the ALT-click action
            };
            args.Verbs.Add(v);
        }
    }

    private void OnStartup(EntityUid uid, MechComponent component, ComponentStartup args)
    {
        component.RiderSlot = _container.EnsureContainer<ContainerSlot>(uid, component.RiderSlotId);
        UpdateAppearance(uid, component);
    }

    private void OnDestruction(EntityUid uid, MechComponent component, DestructionEventArgs args)
    {
        TryEject(uid, component);
    }

    private void SetupUser(EntityUid uid, EntityUid user)
    {
        var rider = EnsureComp<MechPilotComponent>(user);
        var relay = EnsureComp<RelayInputMoverComponent>(user);
        _mover.SetRelay(user, uid, relay);
        rider.Mech = uid;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<MechPilotComponent>(uid))
            return;
        RemComp<RelayInputMoverComponent>(uid);
    }

    public bool IsEmpty(MechComponent component)
    {
        return component.RiderSlot.ContainedEntity == null;
    }

    public bool CanInsert(EntityUid uid, EntityUid toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component) && _actionBlocker.CanMove(toInsert) && HasComp<BodyComponent>(toInsert);
    }

    public bool TryInsert(EntityUid uid, EntityUid? toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (toInsert == null || component.RiderSlot.ContainedEntity == toInsert)
            return false;

        if (!CanInsert(uid, toInsert.Value, component))
            return false;

        SetupUser(uid, toInsert.Value);
        component.RiderSlot.Insert(toInsert.Value, EntityManager);
        UpdateAppearance(uid, component);
        return true;
    }

    public bool TryEject(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.RiderSlot.ContainedEntity == null)
            return false;

        RemoveUser(component.RiderSlot.ContainedEntity.Value);
        _container.RemoveEntity(uid, component.RiderSlot.ContainedEntity.Value);
        UpdateAppearance(uid, component);
        return true;
    }

    public void UpdateAppearance(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _appearance.SetData(uid, MechVisuals.Open, IsEmpty(component));
    }
}
