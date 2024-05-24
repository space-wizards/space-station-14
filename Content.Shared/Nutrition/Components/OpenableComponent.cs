using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// A drink or food that can be opened.
/// Starts closed, open it with Z or E.
/// </summary>
[NetworkedComponent, AutoGenerateComponentState]
[RegisterComponent, Access(typeof(OpenableSystem))]
public sealed partial class OpenableComponent : Component
{
    /// <summary>
    /// Whether this drink or food is opened or not.
    /// Drinks can only be drunk or poured from/into when open, and food can only be eaten when open.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Opened;

    /// <summary>
    /// If this is false you cant press Z to open it.
    /// Requires an OpenBehavior damage threshold or other logic to open.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OpenableByHand = true;

    /// <summary>
    /// Text shown when examining and its open.
    /// </summary>
    [DataField]
    public LocId ExamineText = "drink-component-on-examine-is-opened";

    /// <summary>
    /// The locale id for the popup shown when IsClosed is called and closed. Needs a "owner" entity argument passed to it.
    /// Defaults to the popup drink uses since its "correct".
    /// It's still generic enough that you should change it if you make openable non-drinks, i.e. unwrap it first, peel it first.
    /// </summary>
    [DataField]
    public LocId ClosedPopup = "drink-component-try-use-drink-not-open";

    /// <summary>
    /// Text to show in the verb menu for the "Open" action.
    /// You may want to change this for non-drinks, i.e. "Peel", "Unwrap"
    /// </summary>
    [DataField]
    public LocId OpenVerbText = "openable-component-verb-open";

    /// <summary>
    /// Text to show in the verb menu for the "Close" action.
    /// You may want to change this for non-drinks, i.e. "Wrap"
    /// </summary>
    [DataField]
    public LocId CloseVerbText = "openable-component-verb-close";

    /// <summary>
    /// Sound played when opening.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("canOpenSounds");

    /// <summary>
    /// Can this item be closed again after opening?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Closeable;

    /// <summary>
    /// Sound played when closing.
    /// </summary>
    [DataField]
    public SoundSpecifier? CloseSound;
}
