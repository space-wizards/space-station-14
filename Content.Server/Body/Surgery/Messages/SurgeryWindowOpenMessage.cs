using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Body.Surgery.Messages
{
    public class SurgeryWindowOpenMessage : ComponentMessage
    {
        public SurgeryWindowOpenMessage(SurgeryToolComponent tool)
        {
            Tool = tool;
        }

        public SurgeryToolComponent Tool { get; }
    }
}
