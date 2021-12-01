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
    public class TypingIndicatorSystem : SharedTypingIndicatorSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly Dictionary<EntityUid, TypingIndicatorGui> _guis = new();
        private IEntity? _attachedEntity;

        /// <summary>
        /// The system needs to be enabled by default client side for handling remote players
        /// but clients can opt in to use this feature.
        /// </summary>
		public bool EnabledLocally => _cfg.GetCVar(CCVars.ChatTypingIndicatorSystemEnabled);

        /// <summary>
        /// Used for throttling how often we allow the client
        /// to send messages informing the server they are typing.
        /// </summary>
        private bool _canTransmit = false;

        public override void Initialize()
        {
            base.Initialize();
            _canTransmit = true;
            SubscribeNetworkEvent<RemoteClientTypingMessage>(HandleRemoteTyping);
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void HandleClientTyping()
        {
            if (!EnabledLocally || !_canTransmit) return;
            RaiseNetworkEvent(new ClientTypingMessage(_playerManager?.LocalPlayer?.UserId, _attachedEntity?.Uid));
            _canTransmit = false;
            Timer.Spawn(1000 * (int) _cfg.GetCVar(CCVars.ChatTypingIndicatorPollRate), () =>
             {
                 _canTransmit = true;
             });
        }

        private void HandleRemoteTyping(RemoteClientTypingMessage ev)
        {
            var entity = EntityManager.GetEntity(ev.EnityId.GetValueOrDefault());
            var comp = entity.EnsureComponent<TypingIndicatorComponent>();

            //a remote client is typing. toggle their component so we can render the overlay locally.
            comp.IsVisible = true;
            Timer.Spawn(3000, () =>
            {
                comp.IsVisible = false;
            });
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage message)
        {
            _attachedEntity = message.AttachedEntity;
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            //this is ripped from debug health overlay. not good but it works.
            if (_attachedEntity == null || _attachedEntity.Deleted)
            {
                return;
            }

            var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);

            foreach (var typingIndicatorComp in EntityManager.EntityQuery<TypingIndicatorComponent>())
            {
                var entity = typingIndicatorComp.Owner;
                if (_attachedEntity.Transform.MapID != entity.Transform.MapID ||
                    !viewBox.Contains(entity.Transform.WorldPosition))
                {
                    if (_guis.TryGetValue(entity.Uid, out var oldGui))
                    {
                        _guis.Remove(entity.Uid);
                        oldGui.Visible = false;
                        oldGui.Dispose();
                    }

                    continue;
                }

                if (_guis.ContainsKey(entity.Uid))
                {
                    if (_guis.TryGetValue(entity.Uid, out var typGui))
                    {
                        typGui.Visible = typingIndicatorComp.IsVisible;
                    }

                    continue;
                }

                var gui = new TypingIndicatorGui(entity);
                _guis.Add(entity.Uid, gui);
            }
        }

        [UsedImplicitly]
        public sealed class EnabledTypingIndicatorSystem : IConsoleCommand
        {
            public string Command => "typingindicator";
            public string Description => "Enables the typing indicator for your character";
            public string Help => "";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var cfg = IoCManager.Resolve<IConfigurationManager>();
                cfg.SetCVar(CCVars.ChatTypingIndicatorSystemEnabled, !cfg.GetCVar(CCVars.ChatTypingIndicatorSystemEnabled));
            }
        }
    }
}
