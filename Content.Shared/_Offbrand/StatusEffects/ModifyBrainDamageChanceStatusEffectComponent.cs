/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(ModifyBrainDamageChanceStatusEffectSystem))]
public sealed partial class ModifyBrainDamageChanceStatusEffectComponent : Component
{
    /// <summary>
    /// Thresholds for how much to modify the chance of taking brain damage. Lowest selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, double> OxygenationModifierThresholds;
}
