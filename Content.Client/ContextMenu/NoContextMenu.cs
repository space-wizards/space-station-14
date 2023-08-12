using Content.Shared.Examine;

namespace Content.Client.ContextMenu;

/// <summary>
/// Blocks an entity from showing up in the context ("right-click") menu.
/// </summary>
/// <seealso cref="NoContextMenuSystem"/>
[RegisterComponent]
public sealed class NoContextMenuComponent : Component
{
}

/// <summary>
/// Implements <see cref="NoContextMenuComponent"/>.
/// </summary>
/// <seealso cref="NoContextMenuComponent"/>
public sealed class NoContextMenuSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<NoContextMenuComponent, ExamineAttemptEvent>(ExamineAttempt);
    }

    private static void ExamineAttempt(EntityUid uid, NoContextMenuComponent component, ExamineAttemptEvent args)
    {
        args.Cancel();
    }
}
