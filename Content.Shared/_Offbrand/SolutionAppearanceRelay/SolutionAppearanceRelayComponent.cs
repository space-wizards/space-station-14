/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.SolutionAppearanceRelay;

[RegisterComponent]
public sealed partial class SolutionAppearanceRelayComponent : Component
{
    [DataField(required: true)]
    public string Solution;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}

[Serializable, NetSerializable]
public enum SolutionAppearanceRelayedVisuals : byte
{
    HasRelay
}
