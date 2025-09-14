/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(BlurryVisionStatusEffectSystem))]
public sealed partial class BlurryVisionStatusEffectComponent : Component
{
    [DataField(required: true)]
    public float Blur;

    [DataField(required: true)]
    public float CorrectionPower;
}
