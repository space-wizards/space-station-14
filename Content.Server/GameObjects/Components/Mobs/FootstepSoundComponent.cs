using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Mobs;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Mobs will only make footstep sounds if they have this component.
    /// </summary>
    [RegisterComponent]
    public class FootstepSoundComponent : Component
    {
        public override string Name => "FootstepSound";
    }
}
