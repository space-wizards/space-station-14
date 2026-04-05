using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Systems;

public sealed class EntityExistsRequirementSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityExistsRequirementComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnAssigned(Entity<EntityExistsRequirementComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var query = AllEntityQuery<TransformComponent>(); // i don't like using Transform for this and I have a feeling there's a better way to do this but I can't find it.
        while (query.MoveNext(out var uid, out _))
        {
            if (_whitelist.IsWhitelistPass(ent.Comp.Whitelist, uid))
                return; // we know the entity exists.
        }

        args.Cancelled = true;
    }
}
