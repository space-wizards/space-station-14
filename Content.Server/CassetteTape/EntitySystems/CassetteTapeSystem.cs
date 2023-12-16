using Content.Server.CassetteTape.Components;
using Content.Shared.CassetteTape;
using JetBrains.Annotations;
using Robust.Shared.Utility;


namespace Content.Server.CassetteTape.EntitySystems;

[UsedImplicitly]
public sealed class CassetteTapeSystem : EntitySystem
{
    // Average speaking speed in English is 2.5 words-per-second.
    // Average English word length is 5 characters, so characters can speak at
    // roughly 13 characters per second.
    public const int CharactersSpokenPerSecond = 13;

    public void SetTapeLength(EntityUid uid, float value, CassetteTapeComponent? tape = null)
    {
        if (!Resolve(uid, ref tape))
            return;

        var old = tape._lengthSeconds;
        tape._lengthSeconds = Math.Max(value, 1); // Tapes will always record at least 1 second of audio.
        if (MathHelper.CloseTo(tape._lengthSeconds, old))
            return;

        // Setting the length blanks out the data.
        tape.StoredAudioData = new();
    }




}
