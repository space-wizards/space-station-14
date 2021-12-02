using System;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

public abstract class SharedTrayScannerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ComponentGetState>(OnTrayScannerGetState);
        SubscribeLocalEvent<TrayScannerComponent, ComponentHandleState>(OnTrayScannerHandleState);
        SubscribeLocalEvent<TrayScannerComponent, UseInHandEvent>(OnTrayScannerUsed);
        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);
    }

    private void OnTrayScannerUsed(EntityUid uid, TrayScannerComponent scanner, UseInHandEvent args)
    {
        ActivateTray(uid, scanner);
    }

    private void OnTrayScannerActivate(EntityUid uid, TrayScannerComponent scanner, ActivateInWorldEvent args)
    {
        ActivateTray(uid, scanner);
    }

    private void ActivateTray(EntityUid uid, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner))
            return;

        ToggleTrayScanner(uid, !scanner.Toggled, scanner);
        if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            appearance.SetData(TrayScannerVisual.Visual, scanner.Toggled == true ? TrayScannerVisual.On : TrayScannerVisual.Off);
        }
    }

    public virtual void ToggleTrayScanner(EntityUid uid, bool state, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner))
            return;

        scanner.Toggled = state;
        scanner.Dirty();

        // RaiseLocalEvent(uid, new TrayScannerToggleEvent(scanner.Toggled));
    }

    private void OnTrayScannerGetState(EntityUid uid, TrayScannerComponent scanner, ref ComponentGetState args)
    {
        args.State = new TrayScannerState(scanner.Toggled);
    }

    private void OnTrayScannerHandleState(EntityUid uid, TrayScannerComponent scanner, ref ComponentHandleState args)
    {
        if (args.Current is not TrayScannerState state)
            return;

        ToggleTrayScanner(uid, state.Toggled, scanner);
    }
}

[Serializable, NetSerializable]
public enum TrayScannerVisual : sbyte
{
    Visual,
    On,
    Off
}
