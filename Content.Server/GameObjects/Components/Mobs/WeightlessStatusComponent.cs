using System;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

[RegisterComponent]
[ComponentReference(typeof(SharedWeightlessStatusComponent))]
/// <summary>
/// Simple component to indicate to players when they are experiencing weightlessness.
/// </summary>
public class WeightlessStatusComponent : SharedWeightlessStatusComponent
{


}
