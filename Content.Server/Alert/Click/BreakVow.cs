using Content.Shared.Alert;
using Content.Server.Abilities.Mime;

namespace Content.Server.Alert.Click
{
    ///<summary>
    /// Break your mime vows
    ///</summary>
    [DataDefinition]
    public sealed partial class BreakVow : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

           if (entManager.TryGetComponent(player, out MimePowersComponent? mimePowers))
           {
                entManager.System<MimePowersSystem>().BreakVow(player, mimePowers);
           }
        }
    }
}
