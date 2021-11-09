using System;
using System.Collections.Generic;
using Content.Client.Chat.UI;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.MobState;
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
		[Dependency] private readonly IGameTiming _timing = default!;
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
		/// Time since the chatbox input field had active input.
		/// </summary>
		public TimeSpan TimeSinceType { get; private set; }

        private bool _flag = false;
        public override void Initialize()
		{
			base.Initialize();
			SubscribeNetworkEvent<ClientTypingMessage>(HandleRemoteTyping);
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void HandleClientTyping()
        {
            TimeSinceType = _timing.RealTime;
            _flag = true;
        }

        public void ResetTypingTime()
		{
			TimeSinceType = TimeSpan.Zero;
		}

		private void HandleRemoteTyping(ClientTypingMessage ev)
		{
			var entity = EntityManager.GetEntity(ev.EnityId.GetValueOrDefault());
			var comp = entity.EnsureComponent<TypingIndicatorComponent>();
            comp.TimeAtTyping = _timing.RealTime;
        }

		private void HandlePlayerAttached(PlayerAttachSysMessage message)
		{
			_attachedEntity = message.AttachedEntity;
		}

		public override void Update(float frameTime)
		{
			base.Update(frameTime);

            if (!EnabledLocally) return; //The user has not opted in to use the typing indicator, do not inform the server they are typing.
            if (_flag)
            {
                var localPlayer = _playerManager.LocalPlayer;
                RaiseNetworkEvent(new ClientTypingMessage(localPlayer?.UserId, _attachedEntity?.Uid));
            }

            var pollRate = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.ChatTypingIndicatorPollRate));
            if (_timing.RealTime.Subtract(TimeSinceType) >= pollRate)
            {
                //user hasn't flipped the flag (typed in a while)
                _flag = false;
            }

		}

		public override void FrameUpdate(float frameTime)
		{
			base.FrameUpdate(frameTime);

            if (_attachedEntity == null || _attachedEntity.Deleted)
			{
				return;
			}

			var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);

			foreach (var (mobState, typingIndicatorComp) in EntityManager.EntityQuery<IMobStateComponent, TypingIndicatorComponent>())
			{

				var entity = mobState.Owner;

				if (_attachedEntity.Transform.MapID != entity.Transform.MapID ||
					!viewBox.Contains(entity.Transform.WorldPosition))
				{
					if (_guis.TryGetValue(entity.Uid, out var oldGui))
					{
						_guis.Remove(entity.Uid);
						oldGui.Dispose();
					}

					continue;
				}

				if (_guis.ContainsKey(entity.Uid))
				{
					if (_guis.TryGetValue(entity.Uid, out var typGui))
                    {
                        var difference = _timing.RealTime.Subtract(typingIndicatorComp.TimeAtTyping);
                        typGui.Visible = difference <= TimeSpan.FromSeconds(3f);
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
