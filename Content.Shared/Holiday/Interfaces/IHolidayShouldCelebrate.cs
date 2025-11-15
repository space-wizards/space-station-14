namespace Content.Shared.Holiday.Interfaces
{
    /// <summary>
    ///     Used to decide if this holiday should be celebrated.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public partial interface IHolidayShouldCelebrate
    {
        bool ShouldCelebrate(DateTime date, HolidayPrototype holiday);
    }
}
