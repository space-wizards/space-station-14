using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Speech.Accents;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.EntitySystems;

public sealed class SharedAccentSystem : EntitySystem
{
    // TODO: It'd be nice to automate this
    public IReadOnlyDictionary<string, IAccent> AccentTypes => new IAccent[]
    {
        new BarkAccent(),
        new BackwardsAccent(),
        new OwOAccent(),
        new GermanAccent(),
        new RussianAccent(),
        new FrenchAccent(),
        new FrontalLispAccent(),
        new MobsterAccent(),
    }.ToFrozenDictionary(x => x.Name, x => x);

    //public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    // TODO: It'd be nice to automate this, too
    public override void Initialize()
    {
        SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Bark"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<BackwardsAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Backwards"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<OwOAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["OwO"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<GermanAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["German"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Russian"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["French"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<FrontalLispAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["FrontalLisp"].GetAccentData(ref ev, c));
        SubscribeLocalEvent<MobsterAccentComponent, AccentGetEvent>((e, c, ev) => AccentTypes["Mobster"].GetAccentData(ref ev, c));
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
