namespace Content.Server.Nuke
{
    /// <summary>
    ///     Paper with a written nuclear code in it.
    ///     Can be used in mapping or admins spawn.
    /// </summary>
    [RegisterComponent]
    public sealed class NukeCodePaperComponent : Component
    {
        /// <summary>
        /// Whether or not the paper contains the codes for only
        /// the local station nuke, or for all nukes that exist.
        /// </summary>
        [DataField("getAllCodes")]
        public bool GetAllCodes;
    }
}
