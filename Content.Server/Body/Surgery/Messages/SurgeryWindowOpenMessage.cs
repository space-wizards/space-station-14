using Content.Server.Body.Surgery.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Surgery.Messages
{
#pragma warning disable 618
    public class SurgeryWindowOpenMessage : ComponentMessage
#pragma warning restore 618
    {
        public SurgeryWindowOpenMessage(SurgeryToolComponent tool)
        {
            Tool = tool;
        }

        public SurgeryToolComponent Tool { get; }
    }
}
