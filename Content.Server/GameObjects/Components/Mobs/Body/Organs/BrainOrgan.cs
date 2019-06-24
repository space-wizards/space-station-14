using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class Brain : Organ
    {
        bool AIControlled = false;

        public override void ApplyOrganData()
        {
            AIControlled = (bool)OrganData["AIControlled"];
        }

        public override void Startup()
        {
        }

        public override void Life() //TODO
        {
            switch (State)
            {
                case OrganState.Healthy:
                    //consumes oxygen?
                    break;
                case OrganState.Damaged:
                    //makes mob speak funny things, hallucinations, etc
                    break;
                case OrganState.Dead:
                    //Decomposition?
                    break;
                case OrganState.Missing:
                    //???
                    break;
            }
        }
    }
}
