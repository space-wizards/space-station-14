namespace Content.Shared.Roles
{
    /// <summary>
    /// Provides special hooks for when jobs get spawned in/equipped.
    /// TODO: This is being/should be utilized by more than jobs, and is really just a way to assign components/implants/status effects upon spawning. Rename this class and its derivatives in the future!
    /// TODO: Move derivatives from Server to Shared, probably.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class JobSpecial
    {
        /// <summary>
        /// Applies the effect upon the entity being spawned.
        /// </summary>
        public abstract void AfterEquip(EntityUid mob);

        /// <summary>
        /// Reverts the effect of the <see cref="AfterEquip"/>.
        /// </summary>
        public abstract void AfterUnequip(EntityUid mob);
    }
}
