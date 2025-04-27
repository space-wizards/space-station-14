using Content.Shared.EntityTable;
using Robust.Shared.Prototypes;

namespace Content.Shared.ComponentTable;

/// <summary>
/// Shared side of the DeliverySystem.
/// This covers for letters/packages, as well as spawning a reward for the player upon opening.
/// </summary>
public sealed class SharedComponentTableSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entTable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentTableComponent, MapInitEvent>(OnTableInit);
    }

    private void OnTableInit(Entity<ComponentTableComponent> ent, ref MapInitEvent args)
    {
        var spawns = _entTable.GetSpawns(ent.Comp.Table);

        foreach (var entity in spawns)
        {
            if (_proto.TryIndex(entity, out var entProto))
            {
                EntityManager.AddComponents(ent, entProto.Components);
            }
        }
    }
}
