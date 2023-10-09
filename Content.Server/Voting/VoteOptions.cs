using Robust.Shared.Player;


namespace Content.Server.Voting
{
    /// <summary>
    ///     Options for creating a vote.
    /// </summary>
    public sealed class VoteOptions
    {
        /// <summary>
        ///     The text that is shown for "who called the vote".
        /// </summary>
        public string InitiatorText { get; set; } = "<placeholder>";

        /// <summary>
        ///     The player that started the vote. Used to keep track of player cooldowns to avoid vote spam.
        /// </summary>
        public ICommonSession? InitiatorPlayer { get; set; }

        /// <summary>
        ///     The shown title of the vote.
        /// </summary>
        public string Title { get; set; } = "<somebody forgot to fill this in lol>";

        /// <summary>
        ///     How long the vote lasts.
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     How long the initiator should be timed out from calling votes. Defaults to duration * 2;
        /// </summary>
        public TimeSpan? InitiatorTimeout { get; set; }

        /// <summary>
        ///     The options of the vote. Each entry is a tuple of the player-shown text,
        ///     and a data object that can be used to keep track of options later.
        /// </summary>
        public List<(string text, object data)> Options { get; set; } = new();

        /// <summary>
        ///     Sets <see cref="InitiatorPlayer"/> and <see cref="InitiatorText"/>
        ///     by setting the latter to the player's name.
        /// </summary>
        public void SetInitiator(ICommonSession player)
        {
            InitiatorPlayer = player;
            InitiatorText = player.Name;
        }

        public void SetInitiatorOrServer(ICommonSession? player)
        {
            if (player != null)
            {
                SetInitiator(player);
            }
            else
            {
                InitiatorText = Loc.GetString("vote-options-server-initiator-text");
            }
        }
    }
}
