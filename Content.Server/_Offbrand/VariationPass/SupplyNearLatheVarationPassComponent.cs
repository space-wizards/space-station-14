/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.VariationPass;

/// <summary>
/// A bodge component to spawn the given entities near the lathe of the given prototype in lieu of mapping effort
/// </summary>
[RegisterComponent]
public sealed partial class SupplyNearLatheVariationPassComponent : Component
{
    [DataField(required: true)]
    public EntProtoId LathePrototype;

    [DataField(required: true)]
    public EntProtoId EntityToSpawn;
}
