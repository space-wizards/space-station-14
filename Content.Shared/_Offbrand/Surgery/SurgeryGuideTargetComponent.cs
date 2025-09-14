/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Surgery;

[RegisterComponent]
public sealed partial class SurgeryGuideTargetComponent : Component;

[Serializable, NetSerializable]
public enum SurgeryGuideUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryGuideStartSurgeryMessage(ProtoId<ConstructionPrototype> prototype) : BoundUserInterfaceMessage
{
    public ProtoId<ConstructionPrototype> Prototype = prototype;
}

[Serializable, NetSerializable]
public sealed class SurgeryGuideStartCleanupMessage() : BoundUserInterfaceMessage;
