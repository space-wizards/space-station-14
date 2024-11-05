using System.Text.RegularExpressions;
using System.Text;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class IlleismAccentSystem : EntitySystem
{
    // I am going to Sec -> NAME is going to Sec
    private static readonly Regex RegexIAmUpper = new(@"\bI\s*AM\b|\bI'?M\b");
    private static readonly Regex RegexIAmLower = new(@"\bi\s*am\b|\bI'?m\b", RegexOptions.IgnoreCase);

    // I have it -> NAME has it
    private static readonly Regex RegexIHaveUpper = new(@"\bI\s*HAVE\b|\bI'?VE\b");
    private static readonly Regex RegexIHaveLower = new(@"\bi\s*have\b|\bI'?ve\b", RegexOptions.IgnoreCase);

    // I do! -> NAME does!
    private static readonly Regex RegexIDoUpper = new(@"\bI\s*DO\b");
    private static readonly Regex RegexIDoLower = new(@"\bi\s*do\b", RegexOptions.IgnoreCase);

    // I don't! -> NAME doesn't!
    private static readonly Regex RegexIDontUpper = new(@"\bI\s+DON'?T\b");
    private static readonly Regex RegexIDontLower = new(@"\bi\s+don'?t\b", RegexOptions.IgnoreCase);

    // I/Myself -> NAME
    private static readonly Regex RegexMyselfUpper = new(@"\bMYSELF\b");
    private static readonly Regex RegexI = new(@"\bI\b|\bmyself\b", RegexOptions.IgnoreCase);

    // Me -> NAME
    private static readonly Regex RegexMeUpper = new(@"\bME\b");
    private static readonly Regex RegexMeLower = new(@"\bme\b", RegexOptions.IgnoreCase);

    // My crowbar -> NAME's crowbar
    // That's mine! -> That's NAME's
    private static readonly Regex RegexMyUpper = new(@"\bMY\b|\bMINE\b");
    private static readonly Regex RegexMyLower = new(@"\bmy\b|\bmine\b", RegexOptions.IgnoreCase);

    // I'll do it -> NAME'll do it
    private static readonly Regex RegexIllUpper = new(@"\bI'LL\b");
    private static readonly Regex RegexIllLower = new(@"\bi'll\b", RegexOptions.IgnoreCase);


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
        if (totalLetters < 2)
        {
            return false;
        }
        return uppercaseLetters > totalLetters / 2;
    }

    private void OnAccent(EntityUid uid, IlleismAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;
        var name = Name(uid).Split(' ')[0];
        var upperName = name.ToUpper();

        // I am going to Sec -> NAME is going to Sec
        message = RegexIAmUpper.Replace(message, upperName + " IS");
        message = RegexIAmLower.Replace(message, name + " is");

        // I have it -> NAME has it
        message = RegexIHaveUpper.Replace(message, upperName + " HAS");
        message = RegexIHaveLower.Replace(message, name + " has");

        // I do! -> NAME does!
        message = RegexIDoUpper.Replace(message, upperName + " DOES");
        message = RegexIDoLower.Replace(message, name + " does");

        // I don't! -> NAME doesn't!
        message = RegexIDontUpper.Replace(message, upperName + " DOESN'T");
        message = RegexIDontLower.Replace(message, name + " doesn't");

		// I'll do it -> NAME will do it
        message = RegexIllUpper.Replace(message, upperName + " WILL");
        message = RegexIllLower.Replace(message, name + " will");

        // I/myself -> NAME
        message = RegexMyselfUpper.Replace(message, upperName);
        if (MostlyUppercase(message))
        {
            message = RegexI.Replace(message, upperName);
        }
        else
        {
            message = RegexI.Replace(message, name);
        }

        // Me -> NAME
        message = RegexMeUpper.Replace(message, upperName);
        message = RegexMeLower.Replace(message, name);

        // My crowbar -> NAME's crowbar
        message = RegexMyUpper.Replace(message, upperName + "'S");
        message = RegexMyLower.Replace(message, name + "'s");

        args.Message = message;
    }
};
