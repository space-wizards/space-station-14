using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class StopBeingPulled : IAlertClick
    {
        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(args.Player))
                return;

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<SharedPullableComponent?>(args.Player, out var playerPullable))
            {
                EntitySystem.Get<SharedPullingSystem>().TryStopPull(playerPullable);
            }
        }
    }
}
