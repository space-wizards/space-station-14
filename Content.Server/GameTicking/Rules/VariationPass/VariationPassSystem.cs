using Content.Server.Station.Systems;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
///     Base class for procedural variation rule passes, which apply some kind of variation to a station,
///     so we simply reduce the boilerplate for the event handling a bit with this.
/// </summary>
public abstract class VariationPassSystem<T> : GameRuleSystem<T>
    where T: IComponent
{
    [Dependency] protected readonly StationSystem Stations = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, StationVariationPassEvent>(ApplyVariation);
    }

    protected bool IsMemberOfStation(Entity<TransformComponent> ent, ref StationVariationPassEvent args)
    {
        return Stations.GetOwningStation(ent, ent.Comp) == args.Station.Owner;
    }

    protected abstract void ApplyVariation(Entity<T> ent, ref StationVariationPassEvent args);
}
