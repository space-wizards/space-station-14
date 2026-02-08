using Content.Server.Ghost.Roles.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server.Ghost.Roles;

public sealed class GhostRoleMasterOnUseSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleMasterOnUseComponent, UseInHandEvent>(OnUseInHand);
    }
    private void OnUseInHand(Entity<GhostRoleMasterOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<GhostRoleComponent>(ent, out var ghostRole))
            return;

        if (ent.Comp.Used)
            return;

        ghostRole.Master = args.User;

        _popup.PopupEntity(Loc.GetString(ent.Comp.UsePopup), ent, args.User, PopupType.Large);

    }
}
