using Content.Server.Body.Surgery.Components;

namespace Content.Server.Body.Surgery.Messages
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
