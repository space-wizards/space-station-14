// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.SurveillanceCamera;

/// <summary>
///     This allows surveillance cameras to speak, if the camera in question
///     has a microphone that listens to speech.
/// </summary>
[RegisterComponent]
public sealed partial class SurveillanceCameraSpeakerComponent : Component
{
    // mostly copied from Speech
    [DataField("speechEnabled")] public bool SpeechEnabled = true;

    [ViewVariables] public float SpeechSoundCooldown = 0.5f;

    public TimeSpan LastSoundPlayed = TimeSpan.Zero;
}
