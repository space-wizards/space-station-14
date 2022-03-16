using Content.Client.Chat.UI;
using Content.Client.Gameplay;
using Content.Client.Viewport;
using Content.Shared.Chat;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Chat
{
    public static class ChatInput
    {
        public static void SetupChatInputHandlers(IInputManager inputManager, ChatBox chatBox)
        {
            inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChat(chatBox)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusLocalChat,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.Local)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusWhisperChat,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.Whisper)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusOOC,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.OOC)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.Admin)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusRadio,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.Radio)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusDeadChat,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.Dead)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusConsoleChat,
                InputCmdHandler.FromDelegate(_ => GameplayState.FocusChannel(chatBox, ChatSelectChannel.Console)));

            inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
                InputCmdHandler.FromDelegate(_ => chatBox.CycleChatChannel(true)));

            inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
                InputCmdHandler.FromDelegate(_ => chatBox.CycleChatChannel(false)));
        }
    }
}
