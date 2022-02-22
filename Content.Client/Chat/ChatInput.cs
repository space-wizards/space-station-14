using Content.Client.Chat.UI;
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
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChat(chatBox)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusLocalChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.Local)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusWhisperChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.Whisper)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusOOC,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.OOC)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.Admin)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusRadio,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.Radio)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusDeadChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.Dead)));

            inputManager.SetInputCommand(ContentKeyFunctions.FocusConsoleChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(chatBox, ChatSelectChannel.Console)));

            inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
                InputCmdHandler.FromDelegate(_ => chatBox.CycleChatChannel(true)));

            inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
                InputCmdHandler.FromDelegate(_ => chatBox.CycleChatChannel(false)));
        }
    }
}
