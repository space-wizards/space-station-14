using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Roles.Components;

namespace Content.Server.Roles;

/// <summary>
///     System responsible for giving a ghost of a paradox clone a name modifier.
/// </summary>
public sealed class ParadoxCloneRoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadoxCloneRoleComponent, MindRelayedEvent<RefreshNameModifiersEvent>>(OnRefreshNameModifiers);
    }

    private void OnRefreshNameModifiers(Entity<ParadoxCloneRoleComponent> ent, ref MindRelayedEvent<RefreshNameModifiersEvent> args)
    {
        var mindId = Transform(ent).ParentUid; // the mind role entity is in a container in the mind entity

        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return;

        // only show for ghosts
        if (!HasComp<GhostComponent>(mindComp.OwnedEntity))
            return;

        if (ent.Comp.NameModifier != null)
            args.Args.AddModifier(ent.Comp.NameModifier.Value, 50);
    }
}
