using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Accents;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.EntitySystems;

public sealed class SharedAccentSystem : EntitySystem
{
    // This regex separates out sentences based on punctuation. Used in multiple accents.
    public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    // TODO: It'd be nice to automate this
    public IReadOnlyDictionary<string, IAccent> AccentTypes => new IAccent[]
    {
        new BarkAccent(),
        new BackwardsAccent(),
        new BleatingAccent(),
        new FrenchAccent(),
        new FrontalLispAccent(),
        new GermanAccent(),
        new LizardAccent(),
        new MobsterAccent(),
        new MonkeyAccent(),
        new MothAccent(),
        new MumbleAccent(),
        new OwOAccent(),
        new ParrotAccent(),
        new PirateAccent(),
        new RatvarianLanguageAccent(),
        new RussianAccent(),
        new ScrambledAccent(),
        new SkeletonAccent(),
        new SlurredAccent(),
        new SouthernAccent(),
        new SpanishAccent(),
        new StutteringAccent(),
    }.ToFrozenDictionary(x => x.Name, x => x);

    // TODO: It'd be nice to automate this, too
    public override void Initialize()
    {
        SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Bark"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<BackwardsAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Backwards"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<BleatingAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Bleating"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["French"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<FrontalLispAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["FrontalLisp"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<GermanAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["German"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Lizard"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<MobsterAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Mobster"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<MonkeyAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Monkey"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Moth"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<MumbleAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Mumble"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<OwOAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["OwO"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<ParrotAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Parrot"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<PirateAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Pirate"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<RatvarianLanguageComponent, AccentGetEvent>((e, c, ev) => AccentTypes["RatvarianLanguage"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Russian"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<ScrambledAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Scrambled"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<SkeletonAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Skeleton"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<SlurredAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Slurred"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<SouthernAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Southern"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Spanish"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<StutteringAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Stuttering"].GetAccentData(ref ev, c));
    }

    /// <summary>
    /// Gets the list of accents attached to an entity.
    /// </summary>
    /// <param name="uid">Entity to query accents for</param>
    /// <returns>A list of accent names</returns>
    public Dictionary<string, Dictionary<string, MarkupParameter>?> GetAccentList(EntityUid uid)
    {
        var accents = new Dictionary<string, Dictionary<string, MarkupParameter>?>();
        var accentEvent = new AccentGetEvent(uid, accents);

        RaiseLocalEvent(uid, accentEvent);
        return accents;
    }

    public bool TryGetAccent(string? accentName, [NotNullWhen(true)] out IAccent? accent)
    {
        if (accentName != null && AccentTypes.TryGetValue(accentName, out var accentType))
        {
            accent = accentType;
            return true;
        }

        accent = null;
        return false;
    }
}

public sealed class AccentGetEvent : EntityEventArgs
{
    /// <summary>
    ///     The entity to apply the accent to.
    /// </summary>
    public EntityUid Entity;

    public Dictionary<string, Dictionary<string, MarkupParameter>?> Accents;

    public AccentGetEvent(EntityUid entity, Dictionary<string, Dictionary<string, MarkupParameter>?> accents)
    {
        Entity = entity;
        Accents = accents;
    }
}
