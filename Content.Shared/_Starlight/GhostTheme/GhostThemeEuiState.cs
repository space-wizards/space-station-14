using Content.Shared.Eui;
using Content.Shared.Roles;
using Content.Shared.Starlight;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Starlight.GhostTheme;

[NetSerializable, Serializable]
public sealed class GhostThemeEuiState : EuiStateBase
{
    public HashSet<string> AvailableThemes { get; set; } = [];
}
[NetSerializable, Serializable]
public sealed class GhostThemeOpenedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class GhostThemeSelectedMessage : EuiMessageBase
{
    public readonly string ID;

    public GhostThemeSelectedMessage(string id)
    {
        ID = id;
    }
}

[Serializable, NetSerializable]
public sealed class GhostThemeColorSelectedMessage : EuiMessageBase
{
    public readonly Color Color;

    public GhostThemeColorSelectedMessage(Color color)
    {
        Color = color;
    }
}