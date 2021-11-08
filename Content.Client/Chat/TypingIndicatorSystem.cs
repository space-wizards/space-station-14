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
using Robust.Shared.Log;
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
		public bool Enabled => _cfg.GetCVar(CCVars.ChatTypingIndicatorSystemEnabled);

		/// <summary>
		/// Time since the chatbox input field had active input.
		/// </summary>
		public TimeSpan TimeSinceType { get; private set; }

		/// <summary>
		/// We need this so we essentially throttle
		/// </summary>
		private TimeSpan _onClientTypeCooldown;

        private bool _shouldTick = false;

		public override void Initialize()
		{
			base.Initialize();
			SubscribeNetworkEvent<ClientTypingMessage>(HandleRemoteTyping);
			SubscribeNetworkEvent<ClientStoppedTypingMessage>(HandleRemoteStoppedTyping);
			SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void HandleClientTyping()
        {
            //As to not flood this from every keystroke, we give it a cooldown of 2 seconds.
            var difference = _timing.RealTime.Subtract(_onClientTypeCooldown);
            if (!Enabled &&  difference >= TimeSpan.FromSeconds(2)) return;
            TimeSinceType = _timing.RealTime;
            _shouldTick = true;
            _onClientTypeCooldown = _timing.RealTime;
        }


        public void ResetTypingTime()
		{
			TimeSinceType = TimeSpan.Zero;
		}

		private void HandleRemoteTyping(ClientTypingMessage ev)
		{
			var entity = EntityManager.GetEntity(ev.EnityId.GetValueOrDefault());
			var comp = entity.EnsureComponent<TypingIndicatorComponent>();
			comp.Enabled = true;
		}

		private void HandleRemoteStoppedTyping(ClientStoppedTypingMessage ev)
		{
			var entity = EntityManager.GetEntity(ev.EnityId.GetValueOrDefault());
			var comp = entity.EnsureComponent<TypingIndicatorComponent>();
			comp.Enabled = false;
		}

		private void HandlePlayerAttached(PlayerAttachSysMessage message)
		{
			_attachedEntity = message.AttachedEntity;
		}

		public override void Update(float frameTime)
		{
			base.Update(frameTime);
            if (!_shouldTick) return;

			var pollRate = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.ChatTypingIndicatorPollRate));
            var player = _playerManager.LocalPlayer;
            if (player == null) return;

			if (_timing.RealTime.Subtract(TimeSinceType) >= pollRate)
			{
                RaiseNetworkEvent(new ClientTypingMessage(player.UserId, player.ControlledEntity?.Uid));
                Logger.Info($"typing: {_timing.RealTime}");
            }
			else
			{
                RaiseNetworkEvent(new ClientStoppedTypingMessage(player.UserId, player.ControlledEntity?.Uid));
                _shouldTick = false;
                Logger.Info($"DONE TYPING!: {_timing.RealTime}");
            }
		}

		public override void FrameUpdate(float frameTime)
		{
			base.FrameUpdate(frameTime);
			if (!Enabled) return;

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
						typGui.Visible = typingIndicatorComp.Enabled;
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
