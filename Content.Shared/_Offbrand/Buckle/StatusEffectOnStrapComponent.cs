/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Buckle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(StatusEffectOnStrapSystem))]
public sealed partial class StatusEffectOnStrapComponent : Component
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;
}
