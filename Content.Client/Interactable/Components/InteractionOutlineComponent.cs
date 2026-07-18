namespace Content.Client.Interactable.Components
{
    [RegisterComponent]
    public sealed partial class InteractionOutlineComponent : Component
    {
        public bool InRange;
        public int LastRenderScale;
        public bool Active;
    }
}
