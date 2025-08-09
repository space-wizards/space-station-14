namespace Content.Shared.Holiday.Interfaces
{
    /// <summary>
    ///     Used to trigger arbitrary code when the holiday is celebrated.
    /// </summary>
    public interface IHolidayCelebrate
    {
        /// <summary>
        ///     This method is called before a round starts.
        ///     Use it to do any fun festive modifications.
        /// </summary>
        void Celebrate(HolidayPrototype holiday);
    }
}
