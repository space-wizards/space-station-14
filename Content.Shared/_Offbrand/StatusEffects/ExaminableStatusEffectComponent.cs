/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(ExaminableStatusEffectSystem))]
public sealed partial class ExaminableStatusEffectComponent : Component
{
    /// <summary>
    /// The desired message to show on examine. The target of this effect will be passed as $target to the message.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;
}
