using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Mech.Equipment.Components;

[RegisterComponent]
public sealed class MechGrabberComponent : Component
{
    [DataField("energyPerGrab")]
    public float EnergyPerGrab = -30;

    [DataField("grabDelay")]
    public float GrabDelay = 2.5f;

    [DataField("depositOffset")]
    public Vector2 DepositOffset = new(0, -1);

    [DataField("maxContents")]
    public int MaxContents = 15;

    [DataField("grabSound")]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    public IPlayingAudioStream? AudioStream;

    public Container ItemContainer = default!;
    public CancellationTokenSource? Token;
}
