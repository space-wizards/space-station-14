using Content.Shared.Administration.Logs;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class AdminLogOnTriggerSystem : XOnTriggerSystem<AdminLogOnTriggerComponent>
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    protected override void OnTrigger(Entity<AdminLogOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _adminLogger.Add(
            ent.Comp.LogType,
            ent.Comp.LogImpact,
            $"{ToPrettyString(args.User)} sent a trigger using {ToPrettyString(ent)}: {Loc.GetString(ent.Comp.Message)}"
            );
        // Intentionally does not handle the event since this shouldn't affect the gamestate.
    }
}
