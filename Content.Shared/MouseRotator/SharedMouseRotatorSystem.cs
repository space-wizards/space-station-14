using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Shared.MouseRotator;

/// <summary>
/// This handles rotating an entity based on mouse location
/// </summary>
/// <see cref="MouseRotatorComponent"/>
public abstract class SharedMouseRotatorSystem : EntitySystem
{
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<RequestMouseRotatorRotationEvent>(OnRequestRotation);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO maybe `ActiveMouseRotatorComponent` to avoid querying over more entities than we need?
        // (if this is added to players)
        // (but arch makes these fast anyway, so)
        var query = EntityQueryEnumerator<MouseRotatorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rotator, out var xform))
        {
            if (rotator.GoalRotation == null)
                continue;

            if (_rotate.TryRotateTo(
                    uid,
                    rotator.GoalRotation.Value,
                    frameTime,
                    rotator.AngleTolerance,
                    MathHelper.DegreesToRadians(rotator.RotationSpeed),
                    xform))
            {
                // Stop rotating if we finished
                rotator.GoalRotation = null;
                Dirty(uid, rotator);
            }
        }
    }

    private void OnRequestRotation(RequestMouseRotatorRotationEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent || !TryComp<MouseRotatorComponent>(ent, out var rotator))
        {
            Log.Error($"User {args.SenderSession.Name} ({args.SenderSession.UserId}) tried setting local rotation without a mouse rotator component attached!");
            return;
        }

        rotator.GoalRotation = msg.Rotation;
        Dirty(ent, rotator);
    }
}
