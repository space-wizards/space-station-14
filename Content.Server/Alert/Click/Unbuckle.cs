using Content.Server.GameObjects.Components.Buckle;
using Content.Shared.Alert;
using Robust.Shared.Serialization;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Unbuckles if player is currently buckled.
    /// </summary>
	[UsedImplicitly]
    public class Unbuckle : IAlertClick
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out BuckleComponent buckle))
            {
                buckle.TryUnbuckle(args.Player);
            }
        }
    }
}
