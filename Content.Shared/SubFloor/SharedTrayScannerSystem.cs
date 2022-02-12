using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

public abstract class SharedTrayScannerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ComponentGetState>(OnTrayScannerGetState);
        SubscribeLocalEvent<TrayScannerComponent, ComponentHandleState>(OnTrayScannerHandleState);
        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);
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

    public virtual void OnSubfloorAnchored(EntityUid uid, SubFloorHideComponent? hideComp = null, TransformComponent? xform = null)
    {
    }
}

[Serializable, NetSerializable]
public enum TrayScannerVisual : sbyte
{
    Visual,
    On,
    Off
}
