using JetBrains.Annotations;

namespace Content.Shared.Alert.Click
{
    /// <summary>
    /// Resist fire
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ResistFire : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (entManager.TryGetComponent(player, out FlammableComponent? flammable))
            {
                entManager.System<FlammableSystem>().Resist(player, flammable);
            }
        }
    }
}
