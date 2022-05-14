namespace Content.Server.Holiday.Interfaces
{
    [ImplicitDataDefinitionForInheritors]
    public interface IHolidayShouldCelebrate
    {
        bool ShouldCelebrate(DateTime date, HolidayPrototype holiday);
    }
}
