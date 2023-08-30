using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
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
