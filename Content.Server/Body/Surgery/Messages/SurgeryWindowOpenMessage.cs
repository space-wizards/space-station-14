using Content.Server.Body.Surgery.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Surgery.Messages
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
