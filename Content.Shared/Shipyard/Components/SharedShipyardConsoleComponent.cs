using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.Shipyard;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Shipyard.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedShipyardSystem))]
public sealed class ShipyardConsoleComponent : Component
{
    public static string TargetIdCardSlotId = "ShipyardConsole-targetId";

    [DataField("targetIdSlot")]
    public ItemSlot TargetIdSlot = new();

    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField("shipyardChannel")]
    public string ShipyardChannel = "Command";
}
