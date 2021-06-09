namespace Content.Server.GameObjects.Components.Body.Surgery.Messages
{
    public class SurgeryWindowCloseMessage
    {
        public SurgeryWindowCloseMessage(SurgeryToolComponent tool)
        {
            Tool = tool;
        }

        public SurgeryToolComponent Tool { get; }
    }
}
