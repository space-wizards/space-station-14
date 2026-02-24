using Content.Shared.Defects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// Rolls <see cref="DefectComponent.Prob"/> for each defect on entities with
/// <see cref="RandomDefectsComponent"/> at MapInit, removing defects that fail.
/// </summary>
public sealed class DefectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDefectsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomDefectsComponent> ent, ref MapInitEvent args)
    {
        // Server-authoritative: clients receive the final state after stripping.
        if (_net.IsClient)
            return;

        var toRemove = new List<Type>();

        foreach (var comp in EntityManager.GetComponents(ent.Owner))
        {
            if (comp is not DefectComponent defect)
                continue;

            if (defect.Prob >= 1.0f)
                continue;

            if (!_random.Prob(defect.Prob))
                toRemove.Add(comp.GetType());
        }

        foreach (var type in toRemove)
            EntityManager.RemoveComponent(ent.Owner, type);
    }
}
