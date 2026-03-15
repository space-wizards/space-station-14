using Content.Shared.StatusIcon.Components;
using Robust.Shared.Timing;

namespace Content.Shared.StatusIcon;

public abstract class SharedStatusIconSystem : EntitySystem
{
    // If you are trying to add logic for status icons here, you're probably in the wrong place.
    // Status icons are gathered and rendered entirely clientside.
    // If you wish to use data to render icons, you should replicate that data to the client
    // and subscribe to GetStatusIconsEvent in order to add the relevant icon to a given entity.

    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Adds a temporary StatusIconComponent to the entity if one doesn't exist, and adds a temporary user to the count.
    /// If a permanent StatusIconComponent already exists, it will remain permanent.
    /// </summary>
    public void AddTemporaryStatusIcon(EntityUid id)
    {
        if (_timing.ApplyingState)
            return;

        if (!EnsureComp<StatusIconComponent>(id, out var comp))
        {
            comp.Temporary = true;
        }
        comp.TemporaryUserCount += 1;
    }

    /// <summary>
    /// Reduces the temporary user count for StatusIconComponent,
    /// and removes the component if the count has reached zero and it's temporary.
    /// </summary>
    /// <param name="id"></param>
    public void RemoveTemporaryStatusIcon(EntityUid id)
    {
        if (_timing.ApplyingState)
            return;

        if (TryComp<StatusIconComponent>(id, out var comp))
        {
            comp.TemporaryUserCount -= 1;
            if (comp.TemporaryUserCount <= 0 && comp.Temporary)
            {
                RemComp<StatusIconComponent>(id);
            }
        }
    }
}
