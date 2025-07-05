using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store.Components;
using Robust.Shared.Random;

namespace Content.Server.PDA.Ringer;

/// <summary>
/// Handles the server-side logic for <see cref="SharedRingerSystem"/>.
/// </summary>
public sealed class RingerSystem : SharedRingerSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public const int UplinkCodeGenerationAttempts = 10000;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RingerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RingerComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsert);

        SubscribeLocalEvent<RingerAccessUplinkComponent, GenerateUplinkCodeEvent>(OnGenerateUplinkCode);
    }

    /// <summary>
    /// Randomizes a ringtone for <see cref="RingerComponent"/> on <see cref="MapInitEvent"/>.
    /// </summary>
    private void OnMapInit(Entity<RingerComponent> ent, ref MapInitEvent args)
    {
        UpdateRingerRingtone(ent, GenerateRingtone());
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
        ev.Code = null;
        ent.Comp.Code = null;

        // This will realistically only take one attempt... But you never know...
        for (var i = 0; i < UplinkCodeGenerationAttempts; i++)
        {
            var code = GenerateRingtone();
            if (!TryMatchRingtoneToStore(code, out _))
            {
                // Set the code on the component
                ent.Comp.Code = code;
                // Return the code via the event
                ev.Code = code;
                break;
            }
        }
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
    /// Generates a random ringtone using the C pentatonic scale.
    /// </summary>
    /// <returns>An array of Notes representing the ringtone.</returns>
    /// <remarks>The logic for this is on the Server so that we don't get a different result on the Client every time.</remarks>
    private Note[] GenerateRingtone()
    {
        // Default to using C pentatonic so it at least sounds not terrible.
        return GenerateRingtone(new[]
        {
            Note.C,
            Note.D,
            Note.E,
            Note.F,
            Note.G,
            Note.A,
            Note.B
        });
    }

    /// <summary>
    /// Generates a random ringtone using the specified notes.
    /// </summary>
    /// <param name="notes">The notes to choose from when generating the ringtone.</param>
    /// <returns>An array of Notes representing the ringtone.</returns>
    /// <remarks>The logic for this is on the Server so that we don't get a different result on the Client every time.</remarks>
    private Note[] GenerateRingtone(Note[] notes)
    {
        var ringtone = new Note[RingtoneLength];

        for (var i = 0; i < RingtoneLength; i++)
        {
            ringtone[i] = _random.Pick(notes);
        }

        return ringtone;
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
