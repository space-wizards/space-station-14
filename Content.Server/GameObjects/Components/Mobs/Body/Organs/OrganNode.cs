using Content.Server.Interfaces.GameObjects.Components.Mobs;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     <see cref="Organ"/> doesnt do anything by itself, but <see cref="IBodyFunction"/> does, 
    ///     and OrganNode is an ID that helps to find organ for corresponding body function
    /// </summary>
    public enum OrganNode
    {
        Speech,
        Vision,
        BloodPump,
        Breathing,
        BreathingPath,
        Consciousness,
        BloodFiltrationLiver,
        BloodFiltrationKidney,
        Digestation,
        Appendix,
        Movement,
        Manipulation,
        GasStorage,
        Cell,
        HiveTelepathy
    }
}
