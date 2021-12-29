using Content.Server.Guardian;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Cooldown;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    /// <summary>
    /// Manifests the guardian saved in the action, using the system
    /// </summary>
   [UsedImplicitly]
   [DataDefinition]
    public class ToggleGuardianAction : IInstantAction
    {
        [DataField("cooldown")] public float Cooldown { get; [UsedImplicitly] private set; }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (entManager.TryGetComponent(args.Performer, out GuardianHostComponent? hostComponent) &&
                hostComponent.HostedGuardian != null)
            {
                EntitySystem.Get<GuardianSystem>().ToggleGuardian(hostComponent);
                args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(Cooldown));
            }
            else
            {
                args.Performer.PopupMessage(Loc.GetString("guardian-missing-invalid-action"));
            }
        }
    }
}
