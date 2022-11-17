namespace Content.Shared.Access.Components
{
    /// <summary>
    ///     Stores access levels necessary to "use" an entity
    ///     and allows checking if something or somebody is authorized with these access levels.
    /// </summary>
    [RegisterComponent]
    public sealed class AccessReaderComponent : Component
    {
        /// <summary>
        ///     Whether this reader is enabled or not. If disabled, all access
        ///     checks will pass.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled = true;

        /// <summary>
        ///     The set of tags that will automatically deny an allowed check, if any of them are present.
        /// </summary>
        public HashSet<string> DenyTags = new();

        /// <summary>
        ///     List of access lists to check allowed against. For an access check to pass
        ///     there has to be an access list that is a subset of the access in the checking list.
        /// </summary>
        [DataField("access")]
        public List<HashSet<string>> AccessLists = new();
    }
}
