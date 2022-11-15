using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Mech.EntitySystems;

public abstract class SharedMechSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SharedMechComponent, InteractNoHandEvent>(RelayInteractionEvent);

        SubscribeLocalEvent<SharedMechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SharedMechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<SharedMechComponent, DestructionEventArgs>(OnDestruction);
    }

    private void RelayInteractionEvent<TEvent>(EntityUid uid, SharedMechComponent component, TEvent args) where TEvent : notnull
    {
        Logger.Debug("got interaction");
        foreach (var module in component.Modules)
        {
            RaiseLocalEvent(module, args);
        }
    }

    private void OnAlternativeVerb(EntityUid uid, SharedMechComponent component, GetVerbsEvent<AlternativeVerb> args)
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

    private void OnStartup(EntityUid uid, SharedMechComponent component, ComponentStartup args)
    {
        component.RiderSlot = _container.EnsureContainer<ContainerSlot>(uid, component.RiderSlotId);
        UpdateAppearance(uid, component);
    }

    private void OnDestruction(EntityUid uid, SharedMechComponent component, DestructionEventArgs args)
    {
        TryEject(uid, component);
    }

    private void SetupUser(EntityUid uid, EntityUid user)
    {
        var rider = EnsureComp<MechPilotComponent>(user);
        var relay = EnsureComp<RelayInputMoverComponent>(user);
        var irelay = EnsureComp<InteractionRelayComponent>(user);
        _mover.SetRelay(user, uid, relay);
        _interaction.SetRelay(user, uid, irelay);
        rider.Mech = uid;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<MechPilotComponent>(uid))
            return;
        RemComp<RelayInputMoverComponent>(uid);
        RemComp<InteractionRelayComponent>(uid);
    }

    public bool IsEmpty(SharedMechComponent component)
    {
        return component.RiderSlot.ContainedEntity == null;
    }

    public bool CanInsert(EntityUid uid, EntityUid toInsert, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component) && _actionBlocker.CanMove(toInsert) && HasComp<BodyComponent>(toInsert);
    }

    public virtual bool TryInsert(EntityUid uid, EntityUid? toInsert, SharedMechComponent? component = null)
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

    public virtual bool TryEject(EntityUid uid, SharedMechComponent? component = null)
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

    public void UpdateAppearance(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _appearance.SetData(uid, MechVisuals.Open, IsEmpty(component));
    }
}
