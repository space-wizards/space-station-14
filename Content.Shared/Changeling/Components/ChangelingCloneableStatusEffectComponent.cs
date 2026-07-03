using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// This is used to whitelist the changeling to only clone specific traits or status effects.
/// Works only if the status effect also has <see cref="CloneableStatusEffectComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingCloneableStatusEffectComponent : Component;
