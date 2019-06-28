using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Content.Server.GameObjects.Components;
using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class Brain : Organ
    {
        IChatManager chat;
        List<string> brainDamagePhrases;
        Random random;

        public override void ApplyOrganData()
        {
            //Brain-specific parameters
        }

        public override void Startup()
        {
            base.Startup();
            chat = IoCManager.Resolve<IChatManager>();
            brainDamagePhrases = fillBrainDamageList();
            random = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
        }

        public override void Life(int lifeTick) //TODO
        {
            switch (State)
            {
                case OrganState.Healthy:
                    //consumes oxygen?
                    break;
                case OrganState.Damaged:
                    //makes mob speak funny things, hallucinations, etc
                    if (lifeTick % 60 == 0)
                    {
                        var message = random.Pick(brainDamagePhrases);
                        chat.EntitySay(Owner, message); //TODO: wrap it into another class that could check mob statuses and health states
                    }
                    break;
                case OrganState.Dead:
                    //Decomposition?

                    break;
                case OrganState.Missing:
                    //???
                    break;
            }
        }

        private List<string> fillBrainDamageList()
        {
            return new List<string>(new string[] {
                "HURR DURR IS SPESSMENS READY YET?",
                "FUS RO DAH",
                "fucking 4rries!",
                "stat me",
                ">my face", 
                "roll it easy!", 
                "waaaaaagh!!!", 
                "red wonz go fasta", 
                "FOR TEH EMPRAH", 
                "lol2cat", 
                "dem dwarfs man, dem dwarfs", 
                "SPESS MAHREENS", 
                "hwee did eet fhor khayosss", 
                "lifelike texture ;_;", 
                "luv can bloooom", 
                "PACKETS!!!", 
                "SARAH HALE DID IT!!!", 
                "Don't tell Chase", 
                "not so tough now huh", 
                "WERE NOT BAY!!", 
                "IF YOU DONT LIKE THE CYBORGS OR SLIMES WHY DONT YU O JUST MAKE YORE OWN!", 
                "DONT TALK TO ME ABOUT BALANCE!!!!", 
                "YOU AR JUS LAZY AND DUMB JAMITORS AND SERVICE ROLLS", 
                "BLAME HOSHI!!!", 
                "ARRPEE IZ DED!!!", 
                "THERE ALL JUS MEATAFRIENDS!", 
                "SOTP MESING WITH THE ROUNS SHITMAN!!!", 
                "SKELINGTON IS 4 SHITERS!", 
                "MOMMSI R THE WURST SCUM!!", 
                "How do we engiener=", 
                "try to live freely and automatically good bye", 
                "why woud i take a pin pointner??", 
                "FUCK IT; KISSYOUR ASSES GOOD BYE DEAD MEN! I AM SELFDESTRUCKTING THE STATION!!!!", 
                "How do I set up the. SHow do I set u p the Singu. how I the scrungularity????", 
                "OMG I SED LAW 2 U FAG MOMIM LAW 2!!!"  
            });
        }
    }
}
