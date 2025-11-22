using Content.Shared.Construction.Steps;
using Content.Shared.Whitelist;

namespace Content.Shared._Offbrand.Surgery;

public sealed partial class WhitelistConstructionGraphStep : ArbitraryInsertConstructionGraphStep
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
    {
        var entityWhitelist = entityManager.EntitySysManager.GetEntitySystem<EntityWhitelistSystem>();

        return entityWhitelist.CheckBoth(uid, Blacklist, Whitelist);
    }
}
