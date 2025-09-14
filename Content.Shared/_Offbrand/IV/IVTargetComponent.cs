/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IVSystem))]
public sealed partial class IVTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? IVSource;

    [DataField, AutoNetworkedField]
    public string? IVJointID;
}
