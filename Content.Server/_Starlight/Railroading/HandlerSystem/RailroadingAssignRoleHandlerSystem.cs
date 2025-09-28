using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Components;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingAssignRoleHandlerSystem : EntitySystem
{
    [Dependency] private readonly RoleSystem _roles = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadAssignRoleComponent, RailroadingCardChosenEvent>(OnChosen);
    }

    private void OnChosen(Entity<RailroadAssignRoleComponent> ent, ref RailroadingCardChosenEvent args)
    {
        if(TryComp<MindContainerComponent>(args.Subject, out var mindContainer)
            && mindContainer.Mind.HasValue)
        {
            _roles.MindAddRole(mindContainer.Mind.Value, ent.Comp.Role);
        }
    }
}
