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
    public class ToggleGuarduanAction : IToggleAction
    {
       [DataField("cooldown")] public float Cooldown { get; [UsedImplicitly] private set; }

       public IEntity? Guardian;
       public bool DoToggleAction(ToggleActionEventArgs args)
       {
          if (Guardian == null)
             return false;

          this.Toggle(args.Performer);
          return true;
            
       }

        private void Toggle(IEntity performer)
        {
       
        }
    }
}
