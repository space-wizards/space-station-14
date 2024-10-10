using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class NotCommandRequirementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotCommandRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, NotCommandRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<MindComponent>(args.MindId, out var mind) && mind.OwnedEntity != null)
        {
            if (HasComp<CommandStaffComponent>(mind.OwnedEntity.Value))
                args.Cancelled = true;
        }
    }
}
