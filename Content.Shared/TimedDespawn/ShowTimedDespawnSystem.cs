using Content.Shared.Examine;
using Robust.Shared.Spawners;

namespace Content.Shared.TimedDespawn;

/// <summary>
///     This handles logic relating to <see cref="ShowTimedDespawnComponent"/>.
/// </summary>
public sealed class ShowTimedDespawnSystem : EntitySystem
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowTimedDespawnComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ShowTimedDespawnComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var message = GetMessage(ent.Owner);

        args.PushMarkup(message);
    }

    private string GetMessage(Entity<TimedDespawnComponent?> target)
    {
        var msg = string.Empty;

        // lazy ass convection but anyway better than "23.3123444445555523444444441222222 seconds remain before entity nukes".
        if (Resolve(target, ref target.Comp))
            msg = Loc.GetString("examinable-show-despawn", ("seconds", (int)target.Comp.Lifetime));

        return msg;
    }
}
