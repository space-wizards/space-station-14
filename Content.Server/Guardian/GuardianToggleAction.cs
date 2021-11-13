using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Cooldown;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using System;

namespace Content.Server.Actions.Actions
{
    /// <summary>
    /// Manifests the guardian saved in the action, using the system
    /// </summary>
   [UsedImplicitly]
   [DataDefinition]
    public class ToggleGuarduanAction : IInstantAction
    {
       [DataField("cooldown")] public float Cooldown { get; [UsedImplicitly] private set; }

        public void DoInstantAction(InstantActionEventArgs args)
        {
           if (args.Performer.TryGetComponent<GuardianHostComponent>(out GuardianHostComponent? comp))
           {
                var actionguardian = comp.Hostedguardian;
                EntitySystem.Get<GuardianSystem>().OnGuardianManifestAction(actionguardian, args.Performer.Uid);
                args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(Cooldown));
           }
           else
           {
                args.Performer.PopupMessage(Loc.GetString("guardian-missing-invalid-action"));
           }
        }
    }
}
