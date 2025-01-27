using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(EldritchInfluenceSystem))]
public sealed partial class EldritchInfluenceComponent : Component
{
    [DataField] public bool Spent = false;

    // make sure to update it with the prototype !!!
    [NonSerialized] public static int LayerMask = 69; // 69 idk why not lolol
}
