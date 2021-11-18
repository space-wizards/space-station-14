using System.Collections.Generic;
using System.Linq;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Shared.SubFloor;

public abstract class SharedTrayScannerSystem : EntitySystem
{
    [Dependency] private IEntityLookup _entityLookup = default!;
    [Dependency] private SubFloorHideSystem _subfloorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ComponentGetState>(OnTrayScannerGetState);
        SubscribeLocalEvent<TrayScannerComponent, ComponentHandleState>(OnTrayScannerHandleState);
    }

    public void ToggleTrayScanner(EntityUid uid, bool state, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner))
            return;

        scanner.Toggled = state;
        scanner.Dirty();

        RaiseLocalEvent(uid, new TrayScannerToggleEvent(scanner.Toggled));
    }

    private void OnTrayScannerGetState(EntityUid uid, TrayScannerComponent scanner, ref ComponentGetState args)
    {
        args.State = new TrayScannerState(scanner.Toggled);
    }

    private void OnTrayScannerHandleState(EntityUid uid, TrayScannerComponent scanner, ref ComponentHandleState args)
    {
        if (args.Current is not TrayScannerState state)
            return;

        Logger.DebugS("TrayScannerSystem", $"Handling state now: {scanner.Toggled}");
        ToggleTrayScanner(uid, state.Toggled, scanner);
    }

}

public class TrayScannerToggleEvent : EntityEventArgs
{
    public bool Toggle { get; }

    public TrayScannerToggleEvent(bool toggle)
    {
        Toggle = toggle;
    }
}
