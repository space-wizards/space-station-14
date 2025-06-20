using System.Text;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VowelRotationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VowelRotationComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<VowelRotationComponent> ent, ref AccentGetEvent args)
    {
        var message = args.Message;

        var msgBuilder = new StringBuilder(message);
        for (var i = 0; i < msgBuilder.Length; i++)
        {
            msgBuilder[i] = msgBuilder[i] switch
            {
                'a' => 'e',
                'e' => 'i',
                'i' => 'o',
                'o' => 'u',
                'u' => 'a',
                'A' => 'E',
                'E' => 'I',
                'I' => 'O',
                'O' => 'U',
                'U' => 'A',
                _ => msgBuilder[i]
            };
        }

        args.Message = msgBuilder.ToString();
    }
}
