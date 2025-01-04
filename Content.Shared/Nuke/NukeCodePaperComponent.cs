using Robust.Shared.GameStates;

namespace Content.Shared.Nuke
{
    /// <summary>
    ///     Paper with a written nuclear code in it.
    ///     Can be used in mapping or admins spawn.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedNukeCodePaperSystem))]
    public sealed partial class NukeCodePaperComponent : Component
    {
        /// <summary>
        /// Whether or not paper will contain a code for a nuke on the same
        /// station as the paper, or if it will get a random code from all
        /// possible nukes.
        /// </summary>
        [DataField]
        public bool AllNukesAvailable;

        /// <summary>
        /// The nuke that will get primed using the code on the paper.
        /// </summary>
        [ViewVariables, AutoNetworkedField]
        public EntityUid? Nuke;
    }
}
