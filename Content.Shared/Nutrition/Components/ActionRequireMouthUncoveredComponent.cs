using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(IngestionSystem))]
public sealed partial class ActionRequireMouthUncoveredComponent : Component;
