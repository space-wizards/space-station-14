using Content.Shared.Body.Systems;

namespace Content.Shared.Body.Components;

[RegisterComponent, Access(typeof(BrainSystem))]
public sealed partial class BrainComponent : Component;
