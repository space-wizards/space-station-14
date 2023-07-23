using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using System.Linq;

namespace Content.Client.Overlays;

public abstract class ComponentActivatedClientSystemBase<T> : EntitySystem where T : IComponent
{
    [Dependency] private readonly IPlayerManager _player = default!;

    protected bool IsActive = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<T, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<T, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<T, GotUnequippedEvent>(OnCompUnequip);

        SubscribeLocalEvent<T, GetActivatingComponentsEvent<T>>(OnGetActivatingComponentsEvent);
        SubscribeLocalEvent<T, InventoryRelayedEvent<GetActivatingComponentsEvent<T>>>(UnpackageRelayAndRelay);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public void Activate(IReadOnlyList<T> components)
    {
        IsActive = true;
        OnActivate(components);
    }

    public void Deactivate()
    {
        IsActive = false;
        OnDeactivate();
    }

    protected virtual void OnActivate(IReadOnlyList<T> component) { }

    protected virtual void OnDeactivate() { }

    private void OnStartup(EntityUid uid, T component, ComponentStartup args)
    {
        RefreshOverlay(uid);
    }

    private void OnRemove(EntityUid uid, T component, ComponentRemove args)
    {
        RefreshOverlay(uid);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        RefreshOverlay(args.Entity);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        RefreshOverlay(args.Entity);
    }

    private void OnCompEquip(EntityUid uid, T component, GotEquippedEvent args)
    {
        RefreshOverlay(args.Equipee);
    }

    private void OnCompUnequip(EntityUid uid, T component, GotUnequippedEvent args)
    {
        RefreshOverlay(args.Equipee);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Deactivate();
    }

    private void UnpackageRelayAndRelay(EntityUid uid, T component, InventoryRelayedEvent<GetActivatingComponentsEvent<T>> args)
    {
        OnGetActivatingComponentsEvent(uid, component, args.Args);
    }

    private void OnGetActivatingComponentsEvent(EntityUid uid, T component, GetActivatingComponentsEvent<T> args)
    {
        args.Components.Add(component);
    }

    private void RefreshOverlay(EntityUid uid)
    {
        Deactivate();

        if (uid != _player.LocalPlayer?.ControlledEntity)
        {
            return;
        }

        var ev = new GetActivatingComponentsEvent<T>();
        RaiseLocalEvent(uid, ev);

        if (ev.Components.Any())
        {
            Activate(ev.Components);
        }
    }
}
