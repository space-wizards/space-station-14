using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Shared._Starlight.Abstract.Conditions;

public sealed partial class SubjectWhitelistCondition : BaseCondition
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
    public override bool Handle(EntityUid @subject, EntityUid @object)
    {
        base.Handle(@subject, @object);
        return Ent.System<EntityWhitelistSystem>() is var whitelistSystem
                && !whitelistSystem.IsWhitelistFail(Whitelist, @subject)
                && !whitelistSystem.IsBlacklistPass(Blacklist, @subject);
    }
}
