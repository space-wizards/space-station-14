/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Construction.Steps;
using Content.Shared.Whitelist;

namespace Content.Shared._Offbrand.Surgery;

public sealed partial class WhitelistConstructionGraphStep : ArbitraryInsertConstructionGraphStep
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
    {
        var entityWhitelist = entityManager.EntitySysManager.GetEntitySystem<EntityWhitelistSystem>();

        return entityWhitelist.CheckBoth(uid, Blacklist, Whitelist);
    }
}
