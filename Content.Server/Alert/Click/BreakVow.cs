using Content.Shared.Alert;
using Content.Server.Abilities.Mime;

namespace Content.Server.Alert.Click
{
    ///<summary>
    /// Break your mime vows
    ///</summary>
    [DataDefinition]
    public sealed class BreakVow : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
           if (IoCManager.Resolve<IEntityManager>().TryGetComponent<MimePowersComponent?>(player, out var mimePowers))
           {
                EntitySystem.Get<MimePowersSystem>().BreakVow(player, mimePowers);
           }
        }
    }
}
