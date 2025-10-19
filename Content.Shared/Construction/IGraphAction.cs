// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IGraphAction
    {
        // TODO pass in node/edge & graph ID for better error logs.
        void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager);
    }
}
