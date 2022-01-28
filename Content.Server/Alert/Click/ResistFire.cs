using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Resist fire
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class ResistFire : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out FlammableComponent? flammable))
            {
                EntitySystem.Get<FlammableSystem>().Resist(player, flammable);
            }
        }
    }
}
