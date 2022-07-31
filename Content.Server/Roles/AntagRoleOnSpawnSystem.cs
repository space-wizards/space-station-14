using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Roles;

/// <summary>
/// Assigns an antagonist role to an entity based on the prototype
/// </summary>
public sealed class AddAntagRoleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddAntagRoleComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<AddAntagRoleComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(EntityUid uid, AddAntagRoleComponent component, MindAddedMessage args)
    {
        if (!TryComp<MindComponent>(uid, out var mind) || mind.Mind == null)
            return;

        var roleProto = _proto.Index<AntagPrototype>(component.Prototype);

        var role = new TraitorRole(mind.Mind, roleProto);
        if (!mind.Mind.AllRoles.ToList().Contains(role))
            mind.Mind.AddRole(role);
    }

    private void OnMindRemoved(EntityUid uid, AddAntagRoleComponent component, MindRemovedMessage args)
    {
        if (!TryComp<MindComponent>(uid, out var mind) || mind.Mind == null)
            return;

        var roleProto = _proto.Index<AntagPrototype>(component.Prototype);

        var role = new TraitorRole(mind.Mind, roleProto);
        if (mind.Mind.AllRoles.ToList().Contains(role))
            mind.Mind.RemoveRole(role);
    }
}
