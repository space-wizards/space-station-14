using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    /// <summary>
    ///     Literally just a marker component for footsteps for now.
    /// </summary>
    [RegisterComponent]
    public sealed class CatwalkComponent : Component
    {
        public override string Name => "Catwalk";
    }
}
