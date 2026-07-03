using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Traits.Assorted;

namespace Content.Server.Traits.Assorted;

public sealed class UnborgableSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UnborgableComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<UnborgableComponent> entity, ref MapInitEvent args)
    {
        if (!HasComp<BodyComponent>(entity))
            return;

        if (!_bodySystem.TryGetBodyOrganEntityComps<BrainComponent>(entity.Owner, out var brains))
            return;

        foreach (var brain in brains)
        {
            EnsureComp<MMIIncompatibleComponent>(brain, out var newComp);

            newComp.FailureMessage = entity.Comp.FailureMessage;
        }
    }
}
