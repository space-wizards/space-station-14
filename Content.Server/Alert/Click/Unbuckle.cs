using Content.Server.GameObjects.Components.Buckle;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Unbuckles if player is currently buckled.
    /// </summary>
	[UsedImplicitly]
    [DataDefinition]
    public class Unbuckle : IAlertClick
    {
        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out BuckleComponent? buckle))
            {
                buckle.TryUnbuckle(args.Player);
            }
        }
    }
}
