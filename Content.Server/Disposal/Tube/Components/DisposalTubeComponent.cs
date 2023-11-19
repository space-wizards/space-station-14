using Content.Server.Disposal.Unit.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent]
[Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
public sealed partial class DisposalTubeComponent : Component
{
    [DataField]
    public string ContainerId = "DisposalTube";

    [ViewVariables]
    public bool Connected;

    [DataField]
    public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

    /// <summary>
    ///     Container of entities that are currently inside this tube
    /// </summary>
    [ViewVariables]
    public Container Contents = default!;

    /// <summary>
    /// Damage dealt to containing entities on every turn
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamageOnTurn = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.0 },
        }
    };
}
