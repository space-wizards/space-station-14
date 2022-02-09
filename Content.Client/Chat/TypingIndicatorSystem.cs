using System;
using System.Collections.Generic;
using Content.Client.Chat.UI;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.Chat
{
    public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        //private readonly Dictionary<EntityUid, TypingIndicatorGui> _guis = new();
        private EntityUid? _attachedEntity;

        /// <summary>
        /// The system needs to be enabled by default client side for handling remote players
        /// but clients can opt in to use this feature.
        /// </summary>
        public static bool Toggled;

        /// <summary>
        /// Used for throttling how often we allow the client
        /// to send messages informing the server they are typing.
        /// </summary>
        private bool _canTransmit => Toggled
        && _gameTiming.RealTime.Subtract(_timeSinceLastTransmit) >= TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.ChatTypingIndicatorCooldown));

        private TimeSpan _timeSinceLastTransmit;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<RemoteClientTypingMessage>(HandleRemoteTyping);
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void HandleClientTyping()
        {
            if (_canTransmit)
            {
                RaiseNetworkEvent(new ClientTypingMessage(_playerManager?.LocalPlayer?.UserId, _attachedEntity));
                _timeSinceLastTransmit = _gameTiming.RealTime;
            }
        }

        private void HandleRemoteTyping(RemoteClientTypingMessage ev)
        {
            if (ev.EnityId == null) return;
            var comp = _entityManager.EnsureComponent<TypingIndicatorComponent>(ev.EnityId.Value);
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage message)
        {
            _attachedEntity = message.AttachedEntity;
        }

        public override void Update(float delta)
        {
            base.Update(delta);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
        }

        [UsedImplicitly]
        public sealed class ToggleTypingIndicator : IConsoleCommand
        {
            public string Command => "toggletypingindicator";
            public string Description => "Enables the typing indicator for your character";
            public string Help => "";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                TypingIndicatorSystem.Toggled = !TypingIndicatorSystem.Toggled;
            }
        }
    }
}
