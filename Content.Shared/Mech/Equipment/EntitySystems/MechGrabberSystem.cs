using Content.Shared.Interaction;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;

namespace Content.Shared.Mech.Equipment.EntitySystems;

public sealed class MechGrabberSystem : EntitySystem
{
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechGrabberComponent, InteractNoHandEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, MechGrabberComponent component, InteractNoHandEvent args)
    {
        if (args.Handled || !_interaction.InRangeUnobstructed(args.User, args.ClickLocation))
            return;
        args.Handled = true;

        if (_pulling.IsPulling(args.User))
        {
            var pulled = _pulling.GetPulled(args.User);
            if (TryComp<SharedPullableComponent>(pulled, out var pullable))
                _pulling.TryStopPull(pullable, args.User);
        }
        else if (args.Target != null)
        {
            if (TryComp<SharedPullableComponent>(args.Target.Value, out var pullable))
                _pulling.TogglePull(args.User, pullable);
        }
    }
}
