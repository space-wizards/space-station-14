using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Shared.Traits.Assorted;
using Content.Shared.Jittering;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Traits.Assorted;

public sealed class NyctophobiaSystem : SharedNyctophobiaSystem
{
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    // public override void Initialize()
    // {
    //     base.Initialize();

    // }

    protected override void SetDarkness(EntityUid uid, NyctophobiaComponent nyctophobia, bool inDarkness)
    {
        base.SetDarkness(uid, nyctophobia, inDarkness);

        // nyctophobia.InDarkness = inDarkness;
        // _speedModifier.RefreshMovementSpeedModifiers(uid);
        if (nyctophobia.InDarkness == true)
        {
            EnsureComp<AutoEmoteComponent>(uid);
            _autoEmote.AddEmote(uid, "ScaredScream");
            _stuttering.DoStutter(uid, TimeSpan.FromSeconds(3), true);
            _jittering.DoJitter(uid, TimeSpan.FromSeconds(3), true);
        }
        else
        {
            // Stop random groaning
            _autoEmote.RemoveEmote(uid, "ScaredScream");
        }

        //Dirty(uid, nyctophobia);
    }

}
