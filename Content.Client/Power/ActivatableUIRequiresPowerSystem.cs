using Content.Shared.Power.Components;
using Content.Shared.UserInterface;
using Content.Shared.Wires;

namespace Content.Client.Power;

public sealed class ActivatableUIRequiresPowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, ActivatableUIOpenAttemptEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, ActivatableUIRequiresPowerComponent component, ActivatableUIOpenAttemptEvent args)
    {
        // Client can't predict the power properly at the moment so rely upon the server to do it.
        args.Cancel();
    }
}
