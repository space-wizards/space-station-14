// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Fluids;

namespace Content.Shared.BloodCult;

/// <summary>
/// Allows other systems to handle mop interactions before the default puddle/refillable logic.
/// </summary>
[ByRefEvent]
public struct AbsorbentMopTargetEvent
{
    public EntityUid User { get; }
    public EntityUid Target { get; }
    public EntityUid Used { get; }
    public AbsorbentComponent Component { get; }
    public bool Handled { get; set; }

    public AbsorbentMopTargetEvent(EntityUid user, EntityUid target, EntityUid used, AbsorbentComponent component)
    {
        User = user;
        Target = target;
        Used = used;
        Component = component;
        Handled = false;
    }
}

