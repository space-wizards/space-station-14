using System.Collections.Generic;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    // TODO: Code in-game languages and make this a language
    public class MonkeyAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<MonkeyAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
           
            string[] words = message.Split(' ');
            string accentedMessage = "";

            foreach(string word in words)
            {
                if(_random.NextDouble() >= 0.5)
                {
                    if (word.Length > 1)
                    {
                        for (int i = 0; i < word.Length - 1; i++)
                        {
                            accentedMessage += "o";
                        }
                        if (_random.NextDouble() >= 0.3)
                            accentedMessage += "k";
                    }
                    else
                        accentedMessage += "o";
                }
                else
                {
                    foreach (char letter in word)
                    {
                        if (_random.NextDouble() >= 0.8)
                            accentedMessage += "h";
                        else
                            accentedMessage += "a";
                    }
                    
                }

                accentedMessage += " ";
            }
            accentedMessage = accentedMessage.Substring(0, accentedMessage.Length - 1);
            accentedMessage.Substring(0, 1).Replace(accentedMessage[0], (accentedMessage[0].ToString().ToUpper()).ToCharArray()[0]);
            accentedMessage += "!";

            return accentedMessage;
        }

        private void OnAccent(EntityUid uid, MonkeyAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
