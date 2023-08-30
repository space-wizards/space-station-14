namespace Content.Server.Holiday.Interfaces
{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IHolidayShouldCelebrate
    {
        bool ShouldCelebrate(DateTime date, HolidayPrototype holiday);
    }
}
