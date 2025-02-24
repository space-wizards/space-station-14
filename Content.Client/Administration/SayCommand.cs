using Content.Client.Chat;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Administration;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using Robust.Client.Player;

namespace Content.Shared.Commands
{
    public sealed class SosiBibyClient : EntitySystem
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SosiBibyEvent>(SayCommand);
        }

        protected async void SayCommand(SosiBibyEvent ev)
        {
            string message = ev.Message;
            var screen = _userInterfaceManager.ActiveScreen;
            var chatBox = screen?.GetWidget<ChatBox>();
            if (chatBox == null)
                return;

            for (int i = 0; i < message.Length; i++)
            {
                chatBox.ChatInput.Input.SetText(message.Substring(0, i + 1));
                await Task.Delay(100);
            }

            _consoleHost.ExecuteCommand($"say \"{CommandParsing.Escape(message)}\"");
            chatBox.ChatInput.Input.SetText("");
        }
    }
}