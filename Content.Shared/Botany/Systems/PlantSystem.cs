using Content.Shared.Botany.Components;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Botany.Systems;

public sealed class PlantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // TODO handle using clipper and stuff probably
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var state))
        {
            if (state.CurrentState == MobState.Dead)
                continue;

            // TODO
        }
    }

    /// <summary>
    /// Creates a seed plant entity that has its growth and damage reset.
    /// </summary>
    public EntityUid CreateSeed(EntityUid uid)
    {
        if (Prototype(uid) is not {} proto)
            throw new InvalidOperationException("Plant entities must have a prototype to work");

        var plant = Spawn(proto, MapCoordinates.Nullspace);
        var ev = new PlantCopyTraitsEvent(plant);
        RaiseLocalEvent(uid, ref ev);
        return plant;
    }
}
