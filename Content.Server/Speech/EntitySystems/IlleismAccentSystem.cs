using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class IlleismAccentSystem : EntitySystem
{
    // I am going to Sec -> NAME is going to Sec
    private static readonly Regex RegexIAmUpper = new(@"\bI\s*AM\b");
    private static readonly Regex RegexIAmLower = new(@"\bi\s*am\b", RegexOptions.IgnoreCase);

    // I have it -> NAME has it
    private static readonly Regex RegexIHaveUpper = new(@"\bI\s*HAVE\b");
    private static readonly Regex RegexIHaveLower = new(@"\bi\s*have\b", RegexOptions.IgnoreCase);

    // I do! -> NAME does!
    private static readonly Regex RegexIDoUpper = new(@"\bI\s*DO\b");
    private static readonly Regex RegexIDoLower = new(@"\bi\s*do\b", RegexOptions.IgnoreCase);

    // I don't! -> NAME doesn't!
    private static readonly Regex RegexIDontUpper = new(@"\bI\s+DON'?T\b");
    private static readonly Regex RegexIDontLower = new(@"\bi\s+don'?t\b", RegexOptions.IgnoreCase);

    // I/Myself -> NAME
    private static readonly Regex RegexMyselfUpper = new(@"\bMYSELF\b");
    private static readonly Regex RegexI = new(@"\bI\b|\bmyself\b");

    // Me -> NAME
    private static readonly Regex RegexMeUpper = new(@"\bME\b");
    private static readonly Regex RegexMeLower = new(@"\bme\b", RegexOptions.IgnoreCase);

    // My crowbar -> NAME's crowbar
    // That's mine! -> That's NAME's
    private static readonly Regex RegexMyUpper = new(@"\bMY\b|\bMINE\b");
    private static readonly Regex RegexMyLower = new(@"\bmy\b|\bmine\b", RegexOptions.IgnoreCase);

    // I'll do it -> NAME'll do it
    private static readonly Regex RegexIllUpper = new(@"\bI'?LL\b");
    private static readonly Regex RegexIllLower = new(@"\bi'?ll\b", RegexOptions.IgnoreCase);


    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IlleismAccentComponent, AccentGetEvent>(OnAccent);
    }

    private bool MostlyUppercase(string message)
    {
        int totalLetters = 0;
        int uppercaseLetters = 0;

        // Iterate through each character in the string
        foreach (char c in message)
        {
            if (char.IsLetter(c)) // Check if the character is a letter
            {
                totalLetters++;
                if (char.IsUpper(c)) // Check if the letter is uppercase
                {
                    uppercaseLetters++;
                }
            }
        }
        if (totalLetters == 0)
        {
            return false;
        }
        return uppercaseLetters > totalLetters / 2;
    }

    private void OnAccent(EntityUid uid, IlleismAccentComponent component, AccentGetEvent args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var message = args.Message;

        // I am going to Sec -> NAME is going to Sec
        message = RegexIAmUpper.Replace(message, (Name(uid) + " is").ToUpper());
        message = RegexIAmLower.Replace(message, Name(uid) + " is");

        // I have it -> NAME has it
        message = RegexIHaveUpper.Replace(message, (Name(uid) + " has").ToUpper());
        message = RegexIHaveLower.Replace(message, Name(uid) + " has");

        // I do! -> NAME does!
        message = RegexIDoUpper.Replace(message, (Name(uid) + " does").ToUpper());
        message = RegexIDoLower.Replace(message, Name(uid) + " does");

        // I don't! -> NAME doesn't!
        message = RegexIDontUpper.Replace(message, (Name(uid) + " doesn't").ToUpper());
        message = RegexIDontLower.Replace(message, Name(uid) + " doesn't");

        // I/myself -> NAME
        message = RegexMyselfUpper.Replace(message, Name(uid).ToUpper());
        if (MostlyUppercase(message))
        {
            message = RegexI.Replace(message, Name(uid).ToUpper());
        }
        else
        {
            message = RegexI.Replace(message, Name(uid));
        }

        // Me -> NAME
        message = RegexMeUpper.Replace(message, Name(uid).ToUpper());
        message = RegexMeLower.Replace(message, Name(uid));

        // My crowbar -> NAME's crowbar
        message = RegexMyUpper.Replace(message, (Name(uid) + "'s").ToUpper());
        message = RegexMyLower.Replace(message, Name(uid) + "'s");

        // I'll do it -> NAME'll do it
        message = RegexIllUpper.Replace(message, (Name(uid) + "'ll").ToUpper());
        message = RegexIllLower.Replace(message, Name(uid) + "'ll");

        args.Message = message;
    }
};
