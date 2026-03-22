namespace Content.Shared.Database
{
    /// <summary>
    /// Represents the state of the round when an admin help message was sent.
    /// </summary>
    public enum AdminHelpRoundState : byte
    {
        /// <summary>
        /// The round is in the pre-round lobby
        /// </summary>
        PreRoundLobby = 0,

        /// <summary>
        /// The round is actively in progress
        /// </summary>
        InRound = 1,

        /// <summary>
        /// The round has ended and is in post-round
        /// </summary>
        PostRound = 2
    }
}
