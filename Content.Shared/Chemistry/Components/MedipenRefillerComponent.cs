using Content.Shared.Chemistry.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MedipenRefillerComponent : Component
{
    [DataField("isEmmaged"), ViewVariables(VVAccess.ReadOnly)]
    public bool IsEmmaged;

    [DataField]
    public List<ProtoId<MedipenRecipePrototype>> StaticRecipes = new();

    [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}

public sealed class MedipenRefillerGetRecipesEvent : EntityEventArgs
{
    public readonly EntityUid MedipenRefiller;
    public List<ProtoId<MedipenRecipePrototype>> Recipes = new();

    public MedipenRefillerGetRecipesEvent(EntityUid medipenRefiller)
    {
        MedipenRefiller = medipenRefiller;
    }
}
