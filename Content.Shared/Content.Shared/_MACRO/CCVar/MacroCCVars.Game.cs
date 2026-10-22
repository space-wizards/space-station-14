using Robust.Shared.Configuration;

namespace Content.Shared._MACRO.CCVar;

// Used for variables that, generally speaking, affect the game broadly.
// This is vague, hard to define, and kind of arbitrary, but I personally apply it to CVars that affect players
// both in and out of the lobby.

public sealed partial class MacroCCVars
{
    /// <summary>
    ///     The prototype to use for announcer weights.
    /// </summary>
    public static readonly CVarDef<string> AnnouncerWeightPrototype =
        CVarDef.Create("macrocosm.game.announcer_weight_prototype", "Announcers", CVar.SERVERONLY);
}
