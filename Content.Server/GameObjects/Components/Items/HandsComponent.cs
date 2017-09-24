using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace Content.Server.Interfaces.GameObjects
{
    public class HandsComponent : Component, IHandsComponent
    {
        public override string Name => "Hands";
    }
}
