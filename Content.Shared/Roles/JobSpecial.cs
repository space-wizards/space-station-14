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
        public abstract void AfterEquip(EntityUid mob);
    }
}
