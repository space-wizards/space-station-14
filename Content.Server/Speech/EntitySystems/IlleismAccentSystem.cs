using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class IlleismAccentSystem : EntitySystem
{
    // I am going to Sec -> Bean is going to Sec
    private static readonly Regex RegexIAmUpper = new(@"\bI\s*AM\b");
    private static readonly Regex RegexIAmLower = new(@"\bi\s*am\b", RegexOptions.IgnoreCase);

    // I have it -> Bean has it
    private static readonly Regex RegexIHaveUpper = new(@"\bI\s*HAVE\b");
    private static readonly Regex RegexIHaveLower = new(@"\bi\s*have\b", RegexOptions.IgnoreCase);

    // I do! -> Bean does!
    private static readonly Regex RegexIDoUpper = new(@"\bI\s*DO\b");
    private static readonly Regex RegexIDoLower = new(@"\bi\s*do\b", RegexOptions.IgnoreCase);

    // I don't! -> Bean doesn't!
    private static readonly Regex RegexIDontUpper = new(@"\bI\s+DON'?T\b");
    private static readonly Regex RegexIDontLower = new(@"\bi\s+don'?t\b", RegexOptions.IgnoreCase);

    // I -> Bean
    private static readonly Regex RegexI = new(@"\bI\b");

    // Me -> Bean
    private static readonly Regex RegexMeUpper = new(@"\bME\b");
    private static readonly Regex RegexMeLower = new(@"\bme\b", RegexOptions.IgnoreCase);

    // My crowbar -> Bean's crowbar
    private static readonly Regex RegexMyUpper = new(@"\bMY\b");
    private static readonly Regex RegexMyLower = new(@"\bmy\b\b", RegexOptions.IgnoreCase);


    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IlleismAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, IlleismAccentComponent component, AccentGetEvent args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var message = args.Message;

        // I am going to Sec -> Bean is going to Sec
        message = RegexIAmUpper.Replace(message, (Name(uid) + " is").ToUpper());
        message = RegexIAmLower.Replace(message, Name(uid) + " is");

        // I have it -> Bean has it
        message = RegexIHaveUpper.Replace(message, (Name(uid) + " has").ToUpper());
        message = RegexIHaveLower.Replace(message, Name(uid) + " has");

        // I do! -> Bean does!
        message = RegexIDoUpper.Replace(message, (Name(uid) + " does").ToUpper());
        message = RegexIDoLower.Replace(message, Name(uid) + " does");

        // I don't! -> Bean doesn't!
        message = RegexIDontUpper.Replace(message, (Name(uid) + " doesn't").ToUpper());
        message = RegexIDontLower.Replace(message, Name(uid) + " doesn't");

        // I -> Bean
        message = RegexI.Replace(message, Name(uid));

        // Me -> Bean
        message = RegexMeUpper.Replace(message, Name(uid).ToUpper());
        message = RegexMeLower.Replace(message, Name(uid));

        // My crowbar -> Bean's crowbar
        message = RegexMyUpper.Replace(message, (Name(uid) + "'s").ToUpper());
        message = RegexMyLower.Replace(message, Name(uid) + "'s");

        args.Message = message;
    }
};
