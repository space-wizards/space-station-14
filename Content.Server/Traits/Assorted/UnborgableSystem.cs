using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.Traits.Assorted;

public sealed class UnborgableSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UnborgableComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<UnborgableComponent> entity, ref ComponentStartup args)
    {
        if (!HasComp<BodyComponent>(entity))
            return;

        if (!_bodySystem.TryGetBodyOrganEntityComps<BrainComponent>(entity.Owner, out var brains))
            return;

        foreach (var brain in brains)
        {
            EnsureComp<BrainUnborgableComponent>(brain, out var newComp);

            newComp.FailureMessage = entity.Comp.FailureMessage;
        }
    }
}
