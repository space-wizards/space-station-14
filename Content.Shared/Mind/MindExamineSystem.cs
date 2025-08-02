using Content.Shared.Examine;
using Content.Shared.Mind.Components;

namespace Content.Shared.Mind;

/// <summary>
/// Handles examine text for mind status.
/// </summary>
public sealed class MindExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindStatusComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, MindStatusComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // Check if the entity has MindContainerComponent with ShowExamineInfo enabled
        if (!TryComp<MindContainerComponent>(uid, out var container) || !container.ShowExamineInfo)
            return;

        var text = component.Status switch
        {
            MindStatus.DeadAndIrrecoverable => Loc.GetString("comp-mind-examined-dead-and-irrecoverable", ("ent", uid)),
            MindStatus.DeadAndSSD => Loc.GetString("comp-mind-examined-dead-and-ssd", ("ent", uid)),
            MindStatus.Dead => Loc.GetString("comp-mind-examined-dead", ("ent", uid)),
            MindStatus.Catatonic => Loc.GetString("comp-mind-examined-catatonic", ("ent", uid)),
            MindStatus.SSD => Loc.GetString("comp-mind-examined-ssd", ("ent", uid)),
            MindStatus.Active => null, // No special text for active players
            _ => null
        };

        if (text != null)
        {
            args.PushMarkup($"[color={GetColorForStatus(component.Status)}]{text}[/color]");
        }
    }

    private static string GetColorForStatus(MindStatus status)
    {
        return status switch
        {
            MindStatus.DeadAndIrrecoverable => "mediumpurple",
            MindStatus.DeadAndSSD => "yellow",
            MindStatus.Dead => "red",
            MindStatus.Catatonic => "mediumpurple",
            MindStatus.SSD => "yellow",
            _ => "white"
        };
    }
}
