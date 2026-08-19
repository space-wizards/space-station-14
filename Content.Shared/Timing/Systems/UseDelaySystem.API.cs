using System.Diagnostics.CodeAnalysis;
using Content.Shared.Timing.Components;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.Timing.Systems;

public sealed partial class UseDelaySystem
{
    /// <summary>
    /// Sets the Delay Duration of the specified delayID.
    /// If the delayID doesn't exist, it'll be added.
    /// </summary>
    /// <param name="delayed">The entity whose useDelay is being modified.</param>
    /// <param name="length">The new length of the delay.</param>
    /// <param name="id">The specified id of the delay. If null, it'll use the standard delay.</param>
    /// <remarks>This will add the <see cref="Components.UseDelayComponent"/> if it isn't already present on the entity.</remarks>
    [PublicAPI]
    public void SetLength(Entity<UseDelayComponent?> delayed, TimeSpan length, string id = DefaultId)
    {
        EnsureComp<UseDelayComponent>(delayed.Owner, out var comp);

        if (comp.Delays.TryGetValue(id, out var entry))
        {
            if (entry.Length == length)
                return;

            entry.Length = length;
        }
        else
        {
            comp.Delays.Add(id, new UseDelayInfo(length));
        }

        Dirty(delayed);
    }

    /// <summary>
    /// Checks whether the entity currently has an active delay with the specified ID.
    /// </summary>
    /// <param name="delayed">The entity with the possibly active useDelay.</param>
    /// <param name="id">The specified id of the delay. If null, it'll use the standard delay.</param>
    /// <returns>True, if the specified delay is active, otherwise false.</returns>
    [PublicAPI]
    public bool IsDelayed(Entity<UseDelayComponent?> delayed, string id = DefaultId)
    {
        if (!Resolve(delayed.Owner, ref delayed.Comp, false))
            return false;

        if (!delayed.Comp.Delays.TryGetValue(id, out var entry))
            return false;

        return entry.EndTime >= _gameTiming.CurTime;
    }

    /// <summary>
    /// Cancels an active delay with the specified delayID.
    /// </summary>
    /// <param name="delayed">The entity whose active delay is to be canceled.</param>
    /// <param name="id">The specified id of the delay. If null, it'll use the standard delay.</param>
    /// <returns>Returns true if it was active and has been canceled, otherwise false.</returns>
    [PublicAPI]
    public bool CancelDelay(Entity<UseDelayComponent> delayed, string id = DefaultId)
    {
        if (!delayed.Comp.Delays.TryGetValue(id, out var entry) || entry.EndTime <= _gameTiming.CurTime)
            return false;

        entry.EndTime = _gameTiming.CurTime;
        Dirty(delayed);
        return true;
    }

    /// <summary>
    /// Tries to get info about the delay with the specified ID. See <see cref="Components.UseDelayInfo"/>.
    /// </summary>
    /// <param name="delayed">The entity with the useDelay.</param>
    /// <param name="info">The info about the specified useDelay.</param>
    /// <param name="id">The specified id of the delay. If null, it'll use the standard delay.</param>
    /// <returns>Returns true if the entity has the delay, otherwise false.</returns>
    [PublicAPI]
    public bool TryGetDelayInfo(Entity<UseDelayComponent?> delayed, [NotNullWhen(true)] out UseDelayInfo? info, string id = DefaultId)
    {
        if (!Resolve(delayed.Owner, ref delayed.Comp, false))
        {
            info = null;
            return false;
        }

        return delayed.Comp.Delays.TryGetValue(id, out info);
    }

    /// <summary>
    /// Returns the info about the last ending, active delay.
    /// </summary>
    /// <param name="delayed">The entity with the delays.</param>
    /// <param name="info">The info of the active delay.</param>
    /// <returns>Returns true if one is present and active, otherwise false.</returns>
    [PublicAPI]
    public bool GetLastActiveDelay(Entity<UseDelayComponent> delayed, out UseDelayInfo info)
    {
        var success = false;
        info = new UseDelayInfo();

        foreach (var entry in delayed.Comp.Delays)
        {
            if (entry.Value.EndTime <= _gameTiming.CurTime || entry.Value.EndTime <= info.EndTime)
                continue;

            info = entry.Value;
            success = true;
        }
        return success;
    }

    /// <summary>
    /// Try to reset the specified delay of the entity.
    /// </summary>
    /// <param name="delayed">The entity whose specified delay is being reset.</param>
    /// <param name="checkDelayed">Whether to check if the delay is already active. If so, it'll not reset it.</param>
    /// <param name="id">The specified id of the delay. If null, it'll use the standard delay.</param>
    /// <returns>Returns true if it was able to find & reset the delay, otherwise false.</returns>
    [PublicAPI]
    public bool TryResetDelay(Entity<UseDelayComponent?> delayed, bool checkDelayed = false, string id = DefaultId)
    {
        if (!Resolve(delayed, ref delayed.Comp, false))
            return false;

        if (checkDelayed && IsDelayed(delayed, id))
            return false;

        if (!delayed.Comp.Delays.TryGetValue(id, out var entry))
        {
            DebugTools.Assert($"Attempted to reset the {id} delay of {delayed}, but it's missing.");
            return false;
        }

        var curTime = _gameTiming.CurTime;
        entry.StartTime = curTime;
        entry.EndTime = curTime - _metadata.GetPauseTime(delayed) + entry.Length;
        Dirty(delayed);
        return true;
    }

    /// <summary>
    /// Resets all delays on the entity.
    /// </summary>
    [PublicAPI]
    public void ResetAllDelays(Entity<UseDelayComponent> ent)
    {
        var curTime = _gameTiming.CurTime;
        foreach (var entry in ent.Comp.Delays.Values)
        {
            entry.StartTime = curTime;
            entry.EndTime = curTime - _metadata.GetPauseTime(ent) + entry.Length;
        }
        Dirty(ent);
    }
}
