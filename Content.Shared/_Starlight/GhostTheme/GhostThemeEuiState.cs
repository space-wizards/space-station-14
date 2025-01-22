using Content.Shared.Eui;
using Content.Shared.Roles;
using Content.Shared.Starlight.TextToSpeech;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Starlight.GhostTheme;

[NetSerializable, Serializable]
public sealed class GhostThemeEuiState : EuiStateBase
{
}
[NetSerializable, Serializable]
public sealed class GhostThemeOpenedEvent : EntityEventArgs
{
}
