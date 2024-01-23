using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Server.Access.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

namespace Content.Server.Access.Systems;
public sealed class ActivatableUIRequiresAccessSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIRequiresAccessComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnUIOpenAttempt(Entity<ActivatableUIRequiresAccessComponent> component, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<AccessReaderComponent>(component, out var accessReader) && !_access.IsAllowed(args.User, component, accessReader))
        {
            args.Cancel();
            _popupSystem.PopupEntity(Loc.GetString("lock-comp-has-user-access-fail"), component, args.User);
        }
    }
}

