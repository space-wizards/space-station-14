using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Shared.Parallax;

/// <summary>
/// Handles per-map parallax
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ParallaxComponent : Component
{
    // I wish I could use a typeserializer here but parallax is extremely client-dependent.
    [DataField("parallax")]
    public string Parallax = "Default";

    [UsedImplicitly, ViewVariables(VVAccess.ReadWrite)]
    // ReSharper disable once InconsistentNaming
    public string ParallaxVV
    {
        get => Parallax;
        set
        {
            if (value.Equals(Parallax)) return;
            Parallax = value;
            IoCManager.Resolve<IEntityManager>().Dirty(this);
        }
    }
}
