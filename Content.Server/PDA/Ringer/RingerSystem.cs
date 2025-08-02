using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.GameTicking;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.PDA.Ringer;

/// <summary>
/// Handles the server-side logic for <see cref="SharedRingerSystem"/>.
/// </summary>
public sealed class RingerSystem : SharedRingerSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public static Note[] AllowedNotes =
    {
        Note.C,
        Note.D,
        Note.E,
        Note.F,
        Note.G,
        Note.A,
        Note.B
    };

    /// <summary>
    /// Stores the serialized version of any ringtone that can be excluded from new ringtone generations.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<int> ReservedSerializedRingtones = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RingerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RingerComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsert);

        SubscribeLocalEvent<RingerAccessUplinkComponent, GenerateUplinkCodeEvent>(OnGenerateUplinkCode);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(CleanupReserved);

        InitialSetup();
    }

    /// <summary>
    /// Randomizes a ringtone for <see cref="RingerComponent"/> on <see cref="MapInitEvent"/>.
    /// </summary>
    private void OnMapInit(Entity<RingerComponent> ent, ref MapInitEvent args)
    {
        var ringtone = GenerateRingtone();

        ringtone ??= new Note[RingtoneLength] { Note.A, Note.A, Note.A, Note.A, Note.A, Note.A }; // Fallback

        UpdateRingerRingtone(ent, ringtone);
    }

    /// <summary>
    /// Handles the <see cref="CurrencyInsertAttemptEvent"/> for <see cref="RingerUplinkComponent"/>.
    /// </summary>
    private void OnCurrencyInsert(Entity<RingerComponent> ent, ref CurrencyInsertAttemptEvent args)
    {
        // TODO: Store isn't predicted, can't move it to shared
        if (!TryComp<RingerUplinkComponent>(ent, out var uplink))
        {
            args.Cancel();
            return;
        }

        // if the store can be locked, it must be unlocked first before inserting currency. Stops traitor checking.
        if (!uplink.Unlocked)
            args.Cancel();
    }

    /// <summary>
    /// Handles the <see cref="GenerateUplinkCodeEvent"/> for generating an uplink code.
    /// </summary>
    private void OnGenerateUplinkCode(Entity<RingerAccessUplinkComponent> ent, ref GenerateUplinkCodeEvent ev)
    {
        var code = GenerateRingtone(true, true);

        // Set the code on the component
        ent.Comp.Code = code;
        // Return the code via the event
        ev.Code = code;
    }

    private void InitialSetup()
    {
        ReservedSerializedRingtones.Clear();
    }

    /// <inheritdoc/>
    public override bool TryToggleUplink(EntityUid uid, Note[] ringtone, EntityUid? user = null)
    {
        if (!TryComp<RingerUplinkComponent>(uid, out var uplink))
            return false;

        // On the server, we always check if the code matches
        if (!TryMatchRingtoneToStore(ringtone, out var store, uid))
            return false;

        uplink.TargetStore = store;

        return ToggleUplinkInternal((uid, uplink));
    }

    /// <summary>
    /// Generates a random ringtone using the C major scale.
    /// </summary>
    /// <param name="excludeReserved">Exclude any ringtone registered to ReservedSerializedRingtones.</param>
    /// <param name="reserveRingtone">Add the generated ringtone to ReservedSerializedRingtones. Requires ExcludeReserved to be true.</param>
    /// <returns>An array of Notes representing the ringtone.</returns>
    /// <remarks>The logic for this is on the Server so that we don't get a different result on the Client every time.</remarks>
    private Note[]? GenerateRingtone(bool excludeReserved = false, bool reserveRingtone = false)
    {
        // Default to using C major so it at least sounds not terrible.
        return GenerateRingtone(AllowedNotes, excludeReserved, reserveRingtone);
    }

    /// <summary>
    /// Generates a random ringtone using the specified notes.
    /// </summary>
    /// <param name="notes">The notes to choose from when generating the ringtone.</param>
    /// <param name="excludeReserved">Exclude any ringtone registered to ReservedSerializedRingtones.</param>
    /// <param name="reserveRingtone">Add the generated ringtone to ReservedSerializedRingtones. Requires ExcludeReserved to be true.</param>
    /// <returns>An array of Notes representing the ringtone.</returns>
    /// <remarks>The logic for this is on the Server so that we don't get a different result on the Client every time.</remarks>
    private Note[]? GenerateRingtone(Note[] notes, bool excludeReserved = false, bool reserveRingtone = false)
    {
        var excludedRingtones = excludeReserved ? ReservedSerializedRingtones.ToArray() : null;

        var maxPow = Math.Pow(notes.Length, RingtoneLength);
        if (maxPow > int.MaxValue)
        {
            return null;
        }

        var generatedRingtone = NextIntInRangeButExclude(0, Convert.ToInt32(maxPow) - 1, excludedRingtones);

        if (!TryDeserializeRingtone(notes, generatedRingtone, out var ringtone))
            return null;

        if (excludeReserved && reserveRingtone)
            ReservedSerializedRingtones.Add(generatedRingtone);

        return ringtone;
    }

    /// <summary>
    /// Serialize a ringtone, representing it as an Int32.
    /// </summary>
    /// <param name="allowedNotes">The array of notes used to generate the ringtone.</param>
    /// <param name="ringtone">The ringtone which needs to be serialized.</param>
    /// <param name="serializedRingtone">The ringtone in a serialized format.</param>
    /// <returns>Whether the ringtone could be serialized or not.</returns>
    private bool TrySerializeRingtone(Note[] allowedNotes, Note[] ringtone, [NotNullWhen(true)] out int? serializedRingtone)
    {
        var noteLength = allowedNotes.Length;

        // The serialization stores as an Int32, and therefore using Pow risks overshooting the max value, so we check for if that's a risk.
        // If using 12 possible notes, you can have a ringtone sequence of 7 notes safely without overshooting.
        var maxPow = Math.Pow(noteLength, ringtone.Length);
        if (maxPow > int.MaxValue)
        {
            serializedRingtone = null;
            return false;
        }

        var serializationValue = 0;

        for (var i = 0; i < ringtone.Length; i++)
        {
            var pow = Math.Pow(noteLength, i);
            var index = Array.IndexOf(allowedNotes, ringtone[i]);
            if (index == -1)
            {
                serializedRingtone = null;
                return false;
            }

            serializationValue += Convert.ToInt32(pow) * index;
        }

        serializedRingtone = serializationValue;
        return true;
    }

    /// <summary>
    /// Deserialize a serialized ringtone into a Note array.
    /// </summary>
    /// <param name="allowedNotes">The array of notes used to generate the ringtone.</param>
    /// <param name="serializedRingtone">The ringtone in a serialized format.</param>
    /// <param name="ringtone">The ringtone resulting from the deserialization.</param>
    /// <returns>Whether the ringtone could be deserialized or not.</returns>
    private bool TryDeserializeRingtone(Note[] allowedNotes, int serializedRingtone, [NotNullWhen(true)] out Note[]? ringtone)
    {
        var noteLength = allowedNotes.Length;
        ringtone = new Note[RingtoneLength];

        // The serialization stores as an Int32, and therefore using Pow risks overshooting the max value, so we check for if that's a risk.
        // If using 12 possible notes, you can have a ringtone sequence of 7 notes safely without overshooting.
        var maxPow = Math.Pow(noteLength, RingtoneLength);
        if (maxPow > int.MaxValue)
        {
            ringtone = null;
            return false;
        }

        for (var i = 0; i < RingtoneLength; i++)
        {
            var pow = Math.Pow(noteLength, RingtoneLength - 1 - i);
            var powInt = Convert.ToInt32(pow);
            var val = serializedRingtone / powInt;
            if (!AllowedNotes.TryGetValue(val, out var note))
            {
                ringtone = null;
                return false;
            }

            ringtone[RingtoneLength - 1 - i] = note;
            serializedRingtone -= val * powInt;
        }

        return true;
    }

    /// <summary>
    /// Try to get the store entity that has the matching ringer access.
    /// </summary>
    /// <param name="notes">Notes from the ringer.</param>
    /// <param name="store">The store entity, if there is one.</param>
    /// <param name="ringer">The entity providing the code.</param>
    public bool TryMatchRingtoneToStore(Note[] notes, [NotNullWhen(true)] out EntityUid? store, EntityUid? ringer = null)
    {
        var query = EntityQueryEnumerator<RingerAccessUplinkComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Code != null && notes.SequenceEqual(comp.Code))
            {
                if (comp.BoundEntity != null && comp.BoundEntity != ringer)
                    break;

                store = uid;
                return true;
            }
        }

        store = null;
        return false;
    }

    private void CleanupReserved(RoundRestartCleanupEvent ev)
    {
        ReservedSerializedRingtones.Clear();
    }

    private int NextIntInRangeButExclude(int start, int end, int[]? excludes)
    {
        excludes ??= new int[0];
        Array.Sort(excludes);
        var rangeLength = end - start - excludes.Length;
        var randomInt = _random.Next(rangeLength) + start;

        for (var i = 0; i < excludes.Length; i++)
        {
            if (excludes[i] > randomInt)
            {
                return randomInt;
            }

            randomInt++;
        }

        return randomInt;
    }

    public void SetBoundUplinkEntity(Entity<RingerAccessUplinkComponent> entity, EntityUid? targetEntity)
    {
        entity.Comp.BoundEntity = targetEntity;
    }
}

/// <summary>
/// Event raised to generate a new uplink code for a PDA.
/// </summary>
[ByRefEvent]
public record struct GenerateUplinkCodeEvent
{
    /// <summary>
    /// The generated uplink code (filled in by the event handler).
    /// </summary>
    public Note[]? Code;
}
