using Content.Shared.Alert;
using Content.Server.Abilities.Mime;

namespace Content.Server.Alert.Click
{
    ///<summary>
    /// Retake your mime vows
    ///</summary>
    [DataDefinition]
    public sealed class RetakeVow : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

           if (entManager.TryGetComponent<MimePowersComponent?>(player, out var mimePowers))
           {
                entManager.System<MimePowersSystem>().RetakeVow(player, mimePowers);
           }
        }
    }
}
