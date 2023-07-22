using Robust.Shared.Physics.Components;

namespace Content.Server.NewCon.Commands.Physics;

[ConsoleCommand]
public sealed class PhysicsCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation("velocity")]
    public IEnumerable<float> Velocity([PipedArgument] IEnumerable<EntityUid> input)
    {
        var physQuery = _entity.GetEntityQuery<PhysicsComponent>();

        foreach (var ent in input)
        {
            if (!physQuery.TryGetComponent(ent, out var comp))
                continue;

            yield return comp.LinearVelocity.Length;
        }
    }

    [CommandImplementation("angular_velocity")]
    public IEnumerable<float> AngularVelocity([PipedArgument] IEnumerable<EntityUid> input)
    {
        var physQuery = _entity.GetEntityQuery<PhysicsComponent>();

        foreach (var ent in input)
        {
            if (!physQuery.TryGetComponent(ent, out var comp))
                continue;

            yield return comp.AngularVelocity;
        }
    }
}
