/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared._Offbrand.Wounds;
using Content.Shared.Construction;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class ChangeHeartDamage : IGraphAction
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent<HeartrateComponent>(uid, out var heartrate))
        {
            entityManager.System<HeartSystem>().ChangeHeartDamage((uid, heartrate), Amount);
        }
    }
}
