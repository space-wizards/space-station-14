using Content.Shared.Interaction;

namespace Content.Shared.MouseRotator;

/// <summary>
/// This handles rotating an entity based on mouse location
/// </summary>
/// <see cref="MouseRotatorComponent"/>
public abstract class SharedMouseRotatorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<RequestMouseRotatorRotationEvent>(OnRequestRotation);
    }

    private void OnRequestRotation(RequestMouseRotatorRotationEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent || !HasComp<MouseRotatorComponent>(ent))
        {
            Log.Error($"User {args.SenderSession.Name} ({args.SenderSession.UserId}) tried setting local rotation without a mouse rotator component attached!");
            return;
        }

        _rotate.TryFaceAngle(ent, msg.Rotation);
    }
}
