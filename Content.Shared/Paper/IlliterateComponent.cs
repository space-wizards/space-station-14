using Robust.Shared.GameStates;

namespace Content.Shared.Paper
{
    /// <summary>
    /// An entity with this component cannot write on paper.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class IlliterateComponent : Component
    {
        [DataField]
        [AutoNetworkedField]
        public LocId FailWriteMessage = "paper-component-illiterate";
    }
}
