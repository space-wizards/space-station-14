using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.SSDIndicator;

public sealed class SSDExaminableSystem : EntitySystem
{

    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDExaminableComponent, ExaminedEvent>(OnExamined);

    }

    private void OnExamined(Entity<SSDExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ShowExamineInfo || !args.IsInDetailsRange)
            return;

        var dead = _mobState.IsDead(ent);

        // Scenarios:
        // 1. Normal + Dead = Player is dead but still connected
        // 2. SSD + Dead = Player died and disconnected
        // 3. SSD + Alive = Player disconnected while alive (SSD)
        // 4. Catatonic + Dead = Entity is permanently dead with no player ever attached
        // 5. Catatonic + Alive = Entity was never controlled by a player

        switch (ent.Comp.Status)
        {
            case SSDStatus.Normal:
                if (dead)
                    args.PushMarkup($"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", ent))}[/color]");
                break;

            case SSDStatus.SSD:
                if (dead)
                    args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-dead-and-ssd", ("ent", ent))}[/color]");
                else
                    args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", ent))}[/color]");
                break;

            case SSDStatus.Catatonic:
                if (dead)
                    args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-dead-and-irrecoverable", ("ent", ent))}[/color]");
                else
                    args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", ent))}[/color]");
                break;

            default:
                break;
        }
    }
}
