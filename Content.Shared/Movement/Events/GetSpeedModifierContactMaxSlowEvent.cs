using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity to check if it has a max contact slowdown.
/// </summary>
[ByRefEvent]
public record struct GetSpeedModifierContactMaxSlowEvent(EntityUid Uid)
{
    public float MaxSprintSlowdown = 0f;

    public float MaxWalkSlowdown = 0f;

    public void SetIfMax(float valueSprint, float valueWalk)
    {
        MaxSprintSlowdown = MathF.Max(MaxSprintSlowdown, valueSprint);
        MaxWalkSlowdown = MathF.Max(MaxWalkSlowdown, valueWalk)
    }
}
