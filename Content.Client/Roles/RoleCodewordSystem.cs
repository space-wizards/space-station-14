using Content.Shared.Roles;

namespace Content.Client.Roles;
public sealed class RoleCodewordSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RoleCodewordEvent>(SetRoleCodewords);
    }

    private void SetRoleCodewords(RoleCodewordEvent args)
    {
        EntityUid uid = GetEntity(args.Entity);

        RoleCodewordComponent comp = EnsureComp<RoleCodewordComponent>(uid);
        comp.Codewords = args.Codewords;
    }
}
