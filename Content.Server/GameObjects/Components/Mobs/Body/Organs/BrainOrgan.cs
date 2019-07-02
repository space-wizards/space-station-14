using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Mobs.Body.Organs
{
    /// <summary>
    /// Handles everything associated with brain damage and mind-altering states
    /// </summary>
    public class Brain : Organ
    {
        IChatManager chat;
        List<string> brainDamagePhrases;

        public override void Startup()
        {
            base.Startup();
            chat = IoCManager.Resolve<IChatManager>();
        }

        public override void ExposeData(YamlMappingNode mapping)
        {
            brainDamagePhrases = new List<string>();
            foreach (var prot in mapping.GetNode<YamlSequenceNode>("brainDamageLines").Cast<YamlMappingNode>())
            {
                var line = prot.GetNode("line").AsString();
                brainDamagePhrases.Add(line);
            }
        }

        public override void Life(float frameTime) //TODO
        {
            switch (State)
            {
                case BodyPartState n when (n == BodyPartState.Injured || n == BodyPartState.InjuredSeverely):
                    //makes mob speak funny things
                    if (Math.Abs(BodyOwner.TimeSinceUpdate % 30f) < float.Epsilon && _seed.Prob(0.5f))
                    {
                        var message = _seed.Pick(brainDamagePhrases);
                        chat.EntitySay(Owner, message); //TODO: wrap it into another class that could check mob statuses and health states
                    }
                    break;    
            }
        }
    }
}
