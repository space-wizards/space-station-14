using System;
using System.Collections.Generic;
namespace Content.Server.Spaceshroom.Components;

[RegisterComponent]
[Access(typeof(FoodSpaceshroomSystem))]
public sealed class FoodSpaceshroomComponent : Component
{
    [DataField("solution")]
    public string Solution = "food";
}
