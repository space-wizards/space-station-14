using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.Dragon
{
    public abstract class DragonComponent : Component
    {
        /// <summary>
        /// Defines the devour action
        /// </summary>
        [DataField("devourAction", required: true)]
        public EntityTargetAction DevourAction = default!;
    }
    
}
