using Content.Client.UserInterface;

namespace Content.Client.State
{
    /// <summary>
    ///     Client state that has a main viewport.
    /// </summary>
    /// <remarks>
    ///     Used for taking no-UI screenshots (including things like flash overlay).
    /// </remarks>
    public interface IMainViewportState
    {
        public MainViewport Viewport { get; }
    }
}
