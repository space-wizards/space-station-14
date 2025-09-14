/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Damage;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(MeleeModifiersStatusEffectSystem))]
public sealed partial class MeleeModifiersStatusEffectComponent : Component
{
    [DataField]
    public DamageModifierSet? DamageModifier;

    [DataField]
    public float AttackRateMultiplier = 1f;

    [DataField]
    public float AttackRateConstant = 0f;
}
