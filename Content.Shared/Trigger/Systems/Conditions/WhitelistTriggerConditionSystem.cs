using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Whitelist;

namespace Content.Shared.Trigger.Systems.Conditions;

public sealed class WhitelistTriggerConditionSystem : TriggerConditionSystem<WhitelistTriggerConditionComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    protected override void CheckCondition(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        var cancel = !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);
        ModifyEvent(ent, cancel, ref args);
    }
}
