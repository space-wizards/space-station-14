using Content.Shared.Hands.EntitySystems;
using Content.Shared.Engineering.Components;

namespace Content.Server.Engineering.EntitySystems;

public sealed class DisassembleOnAltVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisassembleOnAltVerbComponent, DissembleDoAfterEvent>(OnDissembleDoAfter);
    }

    private void OnDissembleDoAfter(Entity<DisassembleOnAltVerbComponent> entity, ref DissembleDoAfterEvent args)
    {
        var spawnedEnt = SpawnNextToOrDrop(entity.Comp.PrototypeToSpawn, entity.Owner);

        _handsSystem.TryPickup(args.User, spawnedEnt);
        // Only reason this is in server is because this isn't predicted.
        EntityManager.DeleteEntity(entity.Owner);
    }
}
