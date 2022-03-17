using Content.Server.Chat.Managers;
using Content.Server.Communications;
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
        [Dependency] private readonly IChatManager _chat = default!;

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
            // todo: this should probably be handled by fax system
            var wasSent = false;
            var consoles = EntityManager.EntityQuery<CommunicationsConsoleComponent>();
            foreach (var console in consoles)
            {
                if (!EntityManager.TryGetComponent((console).Owner, out TransformComponent? transform))
                    continue;

                var consolePos = transform.MapPosition;
                EntityManager.SpawnEntity("NukeCodePaper", consolePos);

                wasSent = true;
            }

            if (wasSent)
            {
                var msg = Loc.GetString("nuke-component-announcement-send-codes");
                _chat.DispatchStationAnnouncement(msg);
            }

            return wasSent;
        }
    }
}
