namespace Content.Shared.Alert.Click
{
    ///<summary>
    /// Retake your mime vows
    ///</summary>
    [DataDefinition]
    public sealed partial class RetakeVow : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

           if (entManager.TryGetComponent(player, out MimePowersComponent? mimePowers))
           {
                entManager.System<MimePowersSystem>().RetakeVow(player, mimePowers);
           }
        }
    }
}
