using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Interactable
{
    // TODO make changes in icons when different threshold reached
    // e.g. different icons for 10% 50% 100%
    [RegisterComponent]
    public class MatchboxComponent : Component
    {
        public override string Name => "Matchbox";
    }
}
