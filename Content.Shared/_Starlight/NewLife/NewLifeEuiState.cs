using Content.Shared.Eui;
using Content.Shared.Roles;
using Content.Shared.Starlight.TextToSpeech;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Starlight.NewLife;

[NetSerializable, Serializable]
public sealed class NewLifeEuiState : EuiStateBase
{
    public HashSet<int> UsedSlots { get; set; } = [];
}
[NetSerializable, Serializable]
public sealed class NewLifeOpenedEvent : EntityEventArgs
{
}
