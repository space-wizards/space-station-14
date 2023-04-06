using Content.Shared.RCD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.RCD.Components;

public enum RcdMode : byte
{
    Floors,
    Walls,
    Airlock,
    Deconstruct
}

[RegisterComponent]
[Access(typeof(RCDSystem), typeof(RCDAmmoSystem))]
public sealed class RCDComponent : Component
{
    private const int DefaultCharges = 5;

    /// <summary>
    /// Maximum number of charges this RCD can hold
    /// </summary>
    [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxCharges = DefaultCharges;

    /// <summary>
    /// How many charges we have left. You can refill this with RCD ammo.
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
    public int Charges = DefaultCharges;

    /// <summary>
    /// Time taken to do an action like placing a wall
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 2f;

    [DataField("swapModeSound")]
    public SoundSpecifier SwapModeSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");

    [DataField("successSound")]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// What mode are we on? Can be floors, walls, airlock, deconstruct.
    /// </summary>
    [DataField("mode")]
    public RcdMode Mode = RcdMode.Floors;

    /// <summary>
    /// ID of the floor to create when using the floor mode.
    /// </summary>
    [DataField("floor"), ViewVariables(VVAccess.ReadWrite)]
    public string Floor = "FloorSteel";
}

[Serializable, NetSerializable]
public sealed class RCDComponentState : ComponentState
{
    public int MaxCharges;
    public int Charges;
    public float Delay;
    public RcdMode Mode;
    public string Floor;

    public RCDComponentState(int maxCharges, int charges, float delay, RcdMode mode, string floor)
    {
        MaxCharges = maxCharges;
        Charges = charges;
        Delay = delay;
        Mode = mode;
        Floor = floor;
    }
}
