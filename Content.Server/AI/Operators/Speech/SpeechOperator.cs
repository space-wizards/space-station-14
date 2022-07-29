using Content.Server.Chat.Systems;

namespace Content.Server.AI.Operators.Speech
{
    public sealed class SpeakOperator : AiOperator
    {
        private EntityUid _speaker;
        private string _speechString;
        public SpeakOperator(EntityUid speaker, string speechString)
        {
            _speaker = speaker;
            _speechString = speechString;
        }

        public override Outcome Execute(float frameTime)
        {
            var chatSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
            chatSystem.TrySendInGameICMessage(_speaker, _speechString, InGameICChatType.Speak, false);
            return Outcome.Success;
        }
    }
}
