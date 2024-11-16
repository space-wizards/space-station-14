using Content.Shared.Abilities.Goliath;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Energy.Supermatter;

namespace Content.Server.Starlight.Energy.Supermatter;

public sealed class SupermatterSystem : AccUpdateEntitySystem
{
    private readonly Dictionary<EntityUid, Entity<SupermatterComponent>> _supermatters = [];
    public override void Initialize()
    {
        SubscribeLocalEvent<SupermatterComponent, ComponentStartup>(AddSupermatter);
        SubscribeLocalEvent<SupermatterComponent, ComponentShutdown>(RemoveSupermatter);
    }

    private void AddSupermatter(Entity<SupermatterComponent> ent, ref ComponentStartup args) => _supermatters.TryAdd(ent.Owner, ent);
    private void RemoveSupermatter(Entity<SupermatterComponent> ent, ref ComponentShutdown args) => _supermatters.Remove(ent.Owner);

    protected override float Threshold { get; set; } = 1f;
    protected override void AccUpdate()
    {
        foreach (var supermatter in _supermatters)
        {

        }
    }
}