using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;
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
        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out FlammableComponent? flammable))
            {
                flammable.Resist();
            }
        }
    }
}
