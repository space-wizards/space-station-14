using Robust.Shared.GameObjects;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public record DoInsertDisposalUnitEvent(EntityUid User, EntityUid ToInsert, EntityUid Unit);
}
