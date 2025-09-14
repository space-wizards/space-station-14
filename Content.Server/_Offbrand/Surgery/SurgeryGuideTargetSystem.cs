/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Server.Construction;
using Content.Shared._Offbrand.Surgery;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.Surgery;

public sealed class SurgeryGuideTargetSystem : SharedSurgeryGuideTargetSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void OnStartSurgery(Entity<SurgeryGuideTargetComponent> ent, ref SurgeryGuideStartSurgeryMessage args)
    {
        base.OnStartSurgery(ent, ref args);
        if (!_prototype.TryIndex(args.Prototype, out var construction))
            return;

        _construction.SetPathfindingTarget(ent, construction.TargetNode);
    }

    protected override void OnStartCleanup(Entity<SurgeryGuideTargetComponent> ent, ref SurgeryGuideStartCleanupMessage args)
    {
        base.OnStartCleanup(ent, ref args);
        _construction.SetPathfindingTarget(ent, "Base");
    }
}
