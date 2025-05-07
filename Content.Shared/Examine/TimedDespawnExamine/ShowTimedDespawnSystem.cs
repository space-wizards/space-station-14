using Robust.Shared.Utility;
using Robust.Shared.Spawners;

namespace Content.Shared.Examine;

/// <summary>
///     This handles logic relating to <see cref="ShowTimedDespawnComponent"/>.
/// </summary>
public abstract class ShowTimedDespawnSystem : EntitySystem
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowTimedDespawnComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ShowTimedDespawnComponent comp, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var message = GetMessage(uid);

        args.PushMessage(message);
    }

    private FormattedMessage GetMessage(Entity<TimedDespawnComponent?> target)
    {
        var msg = new FormattedMessage();

        if (Resolve(target, ref target.Comp))
            msg.AddMarkupOrThrow(Loc.GetString("examinable-show-despawn", ("type", target.Comp.Lifetime)));

        return msg;
    }
}
