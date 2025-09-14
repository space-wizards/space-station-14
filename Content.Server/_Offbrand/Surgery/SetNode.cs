/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Construction;
using Content.Server.Construction;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.Surgery;

[DataDefinition]
public sealed partial class SetNode : IGraphAction
{
    [DataField(required: true)]
    public string Node;

    [DataField]
    public List<IGraphCondition> RepeatConditions = new();

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var construction = entityManager.System<ConstructionSystem>();
        if (!construction.CheckConditions(uid, RepeatConditions) || RepeatConditions.Count == 0)
        {
            construction.SetPathfindingTarget(uid, null);
        }
        construction.ChangeNode(uid, userUid, Node);
        construction.ResetEdge(uid);
    }
}
