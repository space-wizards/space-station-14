/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(BloodOxygenationModifierStatusEffectSystem))]
public sealed partial class BloodOxygenationModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The minimum lung oxygenation this status effect guarantees
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinimumOxygenation;
}
