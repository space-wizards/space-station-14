using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(BlurryVisionSystem))]
public sealed partial class CycloritesVisionComponent : Component
{
}
