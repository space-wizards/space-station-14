using Content.Shared.Botany.Components;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles spawning produce entities after harvesting the plant.
/// </summary>
public sealed class PlantProduceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantProduceComponent, PlantHarvestedEvent>(OnHarvested);
    }

    private void OnHarvested(Entity<PlantProduceComponent> ent, ref PlantHarvestedEvent args)
    {
        if (args.User is {} user)
            _popup.PopupEntity(ent, Loc.GetString("botany-harvest-success-message", ("name", Name(ent))), user, PopupType.Medium);

        // spawn at map coords to not be parented to the plant or plant holder
        // it gets reparented to the grid later
        var pos = Transform(ent).MapCoordinates;

        for (int i = 0; i < ent.Comp.Yield; i++)
        {
            var uid = Spawn(ent.Comp.Produce, pos);
            _randomHelper.RandomOffset(uid, 0.25f);

            var ev = new ProduceCreatedEvent(uid);
            RaiseLocalEvent(ent, ref ev);
        }
    }
}
