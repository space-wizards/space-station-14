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
        EntityUid mindId = GetEntity(args.Entity);

        var comp = EnsureComp<RoleCodewordComponent>(mindId);
        comp.RoleCodewords[args.RoleKey] = new CodewordsData(args.Color, args.Codewords);
    }
}
