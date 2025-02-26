using Content.Shared.Whitelist;

namespace Content.Shared._Impstation.RemoveComp
{
    /// <summary>
    /// If there is any other possible way to do what you want to do, reconsider using this.
    /// Takes a list of components and removes them from the entity.
    /// Necessary for removing components from entities which are parented.
    /// </summary>
    [RegisterComponent, AutoGenerateComponentState, Access(typeof(RemoveCompSystem))]
    public sealed partial class RemoveCompComponent : Component
    {
        /// <summary>
        /// The list of components to be removed.
        /// I must reiterate, exhaust all other options before using this component.
        /// </summary>
        [DataField("unwantedComponents"), AutoNetworkedField]
        public EntityWhitelist UnwantedComponents = new();
    }
}
