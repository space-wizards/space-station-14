using Robust.Shared.GameObjects;

namespace Content.Client.State
{
    public sealed class OutlineToggleMessage : EntityEventArgs
    {
        public bool Enabled { get; }

        public OutlineToggleMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
