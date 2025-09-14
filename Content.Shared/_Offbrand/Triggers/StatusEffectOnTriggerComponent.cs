/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Duration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UpdateStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SetStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}
