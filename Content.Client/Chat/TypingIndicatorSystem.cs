using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.Chat
{
    public class TypingIndicatorSystem : SharedTypingIndicatorSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;


        /// <summary>
        /// Time since the chatbox input field had active input.
        /// </summary>
        public TimeSpan TimeSinceType { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
        }

        public void HandleClientTyping()
        {
            var player = _playerManager.LocalPlayer;
            if (player == null) return;
            RaiseNetworkEvent(new ClientTypingMessage(player.UserId, player.ControlledEntity?.Uid));
            TimeSinceType = _timing.RealTime;
        }

        public void ResetTypingTime()
        {
            TimeSinceType = TimeSpan.Zero;
        }

        [UsedImplicitly]
        public sealed class EnabledTypingIndicatorSystem : IConsoleCommand
        {
            public string Command => "enabledtypingindicator";
            public string Description => "Enables the typing indicator";
            public string Help => ""; //helpless

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var cfg = IoCManager.Resolve<IConfigurationManager>();
                cfg.SetCVar(CCVars.ChatTypingIndicatorSystemEnabled, !cfg.GetCVar(CCVars.ChatTypingIndicatorSystemEnabled));
            }
        }
    }
}
