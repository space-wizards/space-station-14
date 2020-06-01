using Content.Server.Interfaces.Chat;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces.GameObjects
{
    public interface ISuicideAct
    {
        public SuicideKind Suicide(IEntity victim, IChatManager chat);
    }

    public enum SuicideKind
    {
        Special, //Doesn't damage the mob, used for "weird" suicides like gibbing

        //Damage type suicides
        Brute,
        Heat,
        Cold,
        Acid,
        Toxic,
        Electric
    }
}
