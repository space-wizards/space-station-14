// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    public sealed class ConstructionBeforeDeleteEvent : CancellableEntityEventArgs
    {
        public EntityUid? User;

        public ConstructionBeforeDeleteEvent(EntityUid? user)
        {
            User = user;
        }
    }

    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class DeleteEntity : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var ev = new ConstructionBeforeDeleteEvent(userUid);
            entityManager.EventBus.RaiseLocalEvent(uid, ev);

            if (!ev.Cancelled)
                entityManager.DeleteEntity(uid);
        }
    }
}
