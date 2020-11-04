using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Kitchen
{

    /// <summary>
    /// Simple tag component to whitelist what can be ground/juiced by the reagentgrinder.
    /// </summary>
    [RegisterComponent]

    public class GrindableComponent : Component
    {
        public override string Name => "Grindable";
    }
}
