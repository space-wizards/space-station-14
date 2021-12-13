using Content.Server.Chat.Managers;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Act
{
    [RequiresExplicitImplementation]
    public interface ISuicideAct
    {
        public SuicideKind Suicide(EntityUid victim, IChatManager chat);
    }

    public enum SuicideKind
    {
        Special, //Doesn't damage the mob, used for "weird" suicides like gibbing

        //Damage type suicides
        Blunt,
        Slash,
        Piercing,
        Heat,
        Shock,
        Cold,
        Poison,
        Radiation,
        Asphyxiation,
        Bloodloss
    }
}
