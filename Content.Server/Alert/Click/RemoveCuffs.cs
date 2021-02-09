#nullable enable
using Content.Server.GameObjects.Components.ActionBlocking;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Alert.Click
{
    /// <summary>
    ///     Try to remove handcuffs from yourself
    /// </summary>
    [UsedImplicitly]
    public class RemoveCuffs : IAlertClick
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) {}

        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out CuffableComponent? cuffableComponent))
            {
                cuffableComponent.TryUncuff(args.Player);
            }
        }
    }
}
