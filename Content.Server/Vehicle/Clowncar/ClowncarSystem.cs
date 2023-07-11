using Content.Server.Chat.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Vehicle.Clowncar;
using Content.Shared.Vehicle.Components;

namespace Content.Server.Vehicle.Clowncar;

public sealed class ClowncarSystem : SharedClowncarSystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // TODO this is not in shared because we cant send IC message from there
        SubscribeLocalEvent<ClowncarComponent, ThankRiderActionEvent>(OnThankRider);
    }

    private void OnThankRider(EntityUid uid, ClowncarComponent component, ThankRiderActionEvent args)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicle)
            || vehicle.Rider is not {} rider)
            return;

        component.ThankCounter++;
        var message = Loc.GetString("clowncar-thankrider", ("rider", rider));
        _chatSystem.TrySendInGameICMessage(args.Performer, message, InGameICChatType.Speak, false);
        args.Handled = true;
    }
}
