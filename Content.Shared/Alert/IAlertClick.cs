namespace Content.Shared.Alert
{
    /// <summary>
    /// Defines what should happen when an alert is clicked.
    /// </summary>
    public interface IAlertClick
    {
        /// <summary>
        /// Invoked on server side when user clicks an alert.
        /// </summary>
        /// <param name="player"></param>
        void AlertClicked(EntityUid player);
    }
}
