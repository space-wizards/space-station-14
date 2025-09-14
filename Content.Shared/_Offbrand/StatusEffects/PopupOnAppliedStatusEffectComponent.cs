/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Popups;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(PopupOnAppliedStatusEffectSystem))]
public sealed partial class PopupOnAppliedStatusEffectComponent : Component
{
    [DataField(required: true)]
    public LocId Message;

    [DataField]
    public PopupType VisualType = PopupType.Small;
}
