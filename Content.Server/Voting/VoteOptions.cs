using Content.Server.Voting.Managers;
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
        ///     Which sessions may send a vote. Used when only a subset of players should be able to vote. Defaults to all.
        /// </summary>
        public VoteManager.VoterEligibility VoterEligibility = VoteManager.VoterEligibility.All;

        /// <summary>
        ///     Whether the vote should send and display the number of votes to the clients. Being an admin defaults this option to true for your client.
        /// </summary>
        public bool DisplayVotes = true;

        /// <summary>
        ///     Whether the vote should have an entity attached to it, to be used for things like letting ghosts follow it. 
        /// </summary>
        public NetEntity? TargetEntity = null;

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
