namespace Content.Shared.Holiday.Interfaces
{
    /// <summary>
    ///     Used to display a festive string in chat when the round starts.
    /// </summary>
    public interface IHolidayGreet
    {
        string Greet(HolidayPrototype holiday);
    }
}
