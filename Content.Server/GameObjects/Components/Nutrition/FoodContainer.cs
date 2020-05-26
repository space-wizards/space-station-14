using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public sealed class DrinkFoodContainerComponent : SharedDrinkFoodContainerComponent, IMapInit, IUse
    {
        public override string Name => "DrinkFoodContainer";
        public void MapInit()
        {
            throw new NotImplementedException();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
