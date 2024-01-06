using Content.Server.Communications;
using Robust.Shared.Random;
using System.Text;

namespace Content.Server.Traits.Assorted;

public sealed class IlliterateSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationConsoleAnnouncementEvent>(OnAnnouncement);
    }

    private void OnAnnouncement(ref CommunicationConsoleAnnouncementEvent args)
    {
        if (IsIlliterate(args.Sender))
            args.Text = ScrambleString(args.Text);
    }
    public string ScrambleString(string str)
    {
        var sb = new StringBuilder();

        foreach (var character in str)
        {
            //If this is a letter or number, replace it with a random character
            if (char.IsLetterOrDigit(character))
            {
                sb.Append((char) (97 + _random.Next(0, 26)));
            }
            else
            {
                //Otherwise just pass it, this is to allow drawings
                sb.Append(character);
            }
        }

        return sb.ToString();
    }

    public bool IsIlliterate(EntityUid? entity)
    {
        if (!entity.HasValue)
            return false;

        return HasComp<IlliterateComponent>(entity);
    }
}
