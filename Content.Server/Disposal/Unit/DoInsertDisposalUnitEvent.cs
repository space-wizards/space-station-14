namespace Content.Server.Disposal.Unit
{
    public record DoInsertDisposalUnitEvent(EntityUid? User, EntityUid ToInsert, EntityUid Unit);
}
