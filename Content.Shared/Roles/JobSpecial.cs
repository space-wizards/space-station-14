namespace Content.Shared.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class JobSpecial
    {
        public abstract void AfterEquip(EntityUid mob);
    }
}
