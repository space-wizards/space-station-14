using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Robust.Shared.Random;

namespace Content.Server.Nuke
{
    /// <summary>
    ///     Nuclear code is generated once per round
    ///     One code works for all nukes
    /// </summary>
    public sealed class NukeCodeSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;

        private const int CodeLength = 6;
        public string Code { get; private set; } = default!;

        public override void Initialize()
        {
            base.Initialize();
            GenerateNewCode();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);
        }

        private void OnRestart(RoundRestartCleanupEvent ev)
        {
            GenerateNewCode();
        }

        /// <summary>
        ///     Checks if code is equal to current bombs code
        /// </summary>
        public bool IsCodeValid(string code)
        {
            return code == Code;
        }

        /// <summary>
        ///     Generate a new nuclear bomb code. Replacing old one.
        /// </summary>
        public void GenerateNewCode()
        {
            var ret = "";
            for (var i = 0; i < CodeLength; i++)
            {
                var c = (char) _random.Next('0', '9' + 1);
                ret += c;
            }

            Code = ret;
        }

        /// <summary>
        ///     Send a nuclear code to all communication consoles
        /// </summary>
        /// <returns>True if at least one console received codes</returns>
        public bool SendNukeCodes()
        {
            var wasSent = false;
            var faxes = EntityManager.EntityQuery<FaxMachineComponent>();
            foreach (var fax in faxes)
            {
                var content = Loc.GetString("nuke-paper-content", ("code", Code));
                _faxSystem.Receive(fax.Owner, content, null, fax);
                wasSent = true;
            }

            // TODO: Allow selecting a station for nuke codes
            if (wasSent)
            {
                var msg = Loc.GetString("nuke-component-announcement-send-codes");
                _chatSystem.DispatchGlobalAnnouncement(msg, colorOverride: Color.Red);
            }

            return wasSent;
        }
    }
}
