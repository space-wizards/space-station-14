using System.Text.RegularExpressions;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Shared.Actions;
using Content.Shared.Toggleable;

namespace Content.Server.Speech.EntitySystems;

public sealed class IlleismAccentSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    // I am going -> NAME is going
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
    // That's mine! -> That's NAME's!
    private static readonly Regex RegexMyUpper = new(@"\bMY\b|\bMINE\b");
    private static readonly Regex RegexMyLower = new(@"\bmy\b|\bmine\b", RegexOptions.IgnoreCase);

    // I'll do it -> NAME will do it
    private static readonly Regex RegexIllUpper = new(@"\bI'LL\b");
    private static readonly Regex RegexIllLower = new(@"\bi'll\b", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IlleismAccentComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IlleismAccentComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<IlleismAccentComponent, ToggleActionEvent>(OnToggleAction);

    }

    private void OnMapInit(Entity<IlleismAccentComponent> ent, ref MapInitEvent args)
    {
        var component = ent.Comp;
        _actionContainer.EnsureAction(ent, ref component.ToggleActionEntity, component.ToggleAction);
        _actions.AddAction(ent, ref component.SelfToggleActionEntity, component.ToggleAction);
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

    private void OnToggleAction(Entity<IlleismAccentComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;
        ent.Comp.IllesimStateIndex = (ent.Comp.IllesimStateIndex + 1) % 3;
        _popup.PopupEntity(Loc.GetString("trait-illeism-adjust"), ent.Owner, ent.Owner);
        args.Handled = true;
    }
    private void OnAccent(EntityUid uid, IlleismAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;
        var name = component.IllesimStateIndex == 2 ? Name(uid) : Name(uid).Split(component.IllesimStrings[component.IllesimStateIndex])[0];
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
