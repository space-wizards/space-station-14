using Content.Server.Cuffs.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    ///     Try to remove handcuffs from yourself
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class RemoveCuffs : IAlertClick
    {
        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out CuffableComponent? cuffableComponent))
            {
                cuffableComponent.TryUncuff(args.Player);
            }
        }
    }
}
