using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Speech.Accents;

namespace Content.Shared.Speech.EntitySystems;

public sealed class SharedAccentSystem : EntitySystem
{
    public IReadOnlyDictionary<string, IAccent> AccentTypes => new IAccent[]
    {
        new BarkAccent(),
        new BackwardsAccent(),
        new OwOAccent(),
        new GermanAccent(),
        new RussianAccent(),
        new FrenchAccent(),
    }.ToFrozenDictionary(x => x.Name, x => x);

    //public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>((e, c, ev) => ev.Accents.Add("Bark"));
        SubscribeLocalEvent<BackwardsAccentComponent, AccentGetEvent>((e, c, ev) => ev.Accents.Add("Backwards"));
        SubscribeLocalEvent<OwOAccentComponent, AccentGetEvent>((e, c, ev) => ev.Accents.Add("OwO"));
        SubscribeLocalEvent<GermanAccentComponent, AccentGetEvent>((e, c, ev) => ev.Accents.Add("German"));
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>((e, c, ev) => ev.Accents.Add("Russian"));
        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>((e, c, ev) => ev.Accents.Add("French"));
    }

    /// <summary>
    /// Gets the list of accents attached to an entity.
    /// </summary>
    /// <param name="uid">Entity to query accents for</param>
    /// <returns>A list of accent names</returns>
    public List<string> GetAccentList(EntityUid uid)
    {
        var accents = new List<string>();
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

    public List<string> Accents;

    public AccentGetEvent(EntityUid entity, List<string> accents)
    {
        Entity = entity;
        Accents = accents;
    }
}
