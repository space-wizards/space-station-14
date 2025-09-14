/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StatusEffectTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField, AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public bool Invert;

    [DataField, AutoNetworkedField]
    public bool TargetUser;
}
