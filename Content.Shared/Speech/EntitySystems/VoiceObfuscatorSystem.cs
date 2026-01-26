using Content.Shared.Chat;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
///     System for replacing a speaker's voice with a generic identifier.
/// </summary>
public sealed class VoiceObfuscatorSystem : EntitySystem
{
    [Dependency] private readonly MaskSystem _mask = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<VoiceObfuscatorComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName, held: false);
        SubscribeLocalEvent<VoiceObfuscatorComponent, ExaminedEvent>(OnExamined);
    }

    #region Event Handlers

    private void OnTransformSpeakerName(Entity<VoiceObfuscatorComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (_mask.IsToggled(ent.Owner))
        {
            return;
        }

        args.VoiceName = GetObfuscatedSpeakerName(args.Sender);
    }

    private void OnExamined(Entity<VoiceObfuscatorComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("voice-obfuscator-examine"));
    }

    #endregion

    /// <summary>
    ///     Returns a generic identity representation for the speaker name.
    /// </summary>
    private string GetObfuscatedSpeakerName(Entity<HumanoidProfileComponent?> ent)
    {
        var identity = _identity.GetIdentityRepresentationNoId(ent);
        return Loc.GetString("voice-obfuscator-voice", ("voice", identity.ToStringUnknown()));
    }
}
