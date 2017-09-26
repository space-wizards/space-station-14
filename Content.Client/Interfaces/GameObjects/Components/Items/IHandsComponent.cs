using Content.Client.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace Content.Client.Interfaces.GameObjects
{
    // HYPER SIMPLE HANDS API CLIENT SIDE.
    // To allow for showing the HUD, mostly.
    public interface IHandsComponent
    {
        IEntity GetEntity(string index);
    }
}
