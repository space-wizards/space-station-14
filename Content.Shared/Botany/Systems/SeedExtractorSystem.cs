using Content.Shared.Botany.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles extracting seed packets from produce.
/// </summary>
public sealed class SeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<SeedExtractorComponent> ent, ref InteractUsingEvent args)
    {
        if (!IsPowered(ent, EntityManager))
            return;

        if (!TryComp<ProduceComponent>(args.Used, out var produce))
            return;

        var user = args.User;
        if (!_seed.GetSeedComp<PlantSeedsComponent>(produce.Seed, out var seeds))
        {
            // should never happen, mistake in produce yaml
            _popup.PopupClient(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used), user, user, PopupType.MediumCaution);
            Log.Error($"Invalid produce {ToPrettyString(args.Used):produce} had seed {produce.Seed.Plant} {produce.Seed.Entity} with no PlantSeedsComponent");
            return;
        }

        var packet = seeds.Packet;

        _popup.PopupClient(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)), user, user, PopupType.Medium);

        QueueDel(args.Used);

        var amount = _random.Next(ent.Comp.BaseMinSeeds, ent.Comp.BaseMaxSeeds + 1);
        var coords = Transform(ent).Coordinates;
        for (int i = 1; i <= amount; i++)
        {
            var plant = produce.Seed.Entity is {} entity
                // for the last seed we know it wont be cloned again so just use the entity as-is
                ? (i == amount ? entity : _plant.CreateSeed(entity))
                // if using a default seed just spawn new one
                : Spawn(produce.Seed.Plant, MapCoordinates.Nullspace);

            var packetEnt = Spawn(packet, coords);
            EnsureComp<SeedComponent>(packetEnt).Seed.Entity = plant;
            // TODO: copy paste the rest of the stuff
        }
    }
}
