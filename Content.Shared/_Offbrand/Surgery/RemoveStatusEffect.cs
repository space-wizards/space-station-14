/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Construction;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class RemoveStatusEffect : IGraphAction
{
    [DataField(required: true)]
    public EntProtoId Effect;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var statusEffects = entityManager.System<StatusEffectsSystem>();
        statusEffects.TryRemoveStatusEffect(uid, Effect);
    }
}
