using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.FoldableCard;

public sealed class FoldableCardSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldableCardComponent, GetVerbsEvent<AlternativeVerb>>(AddFoldVerb);
        SubscribeLocalEvent<FoldableCardComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FoldableCardComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<FoldableCardComponent, ComponentInit>(OnFoldableInit);
        SubscribeLocalEvent<FoldableCardComponent, UseInHandEvent>(UseInHand);
        SubscribeLocalEvent<FoldableCardComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, FoldableCardComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(component.CurrentDescription));
    }
    private void UseInHand(EntityUid uid, FoldableCardComponent component, UseInHandEvent args)
    {
        component.IsFolded = !component.IsFolded;
        SetFolded(uid, component, component.IsFolded);
    }
    private void OnGetState(EntityUid uid, FoldableCardComponent component, ref ComponentGetState args)
    {
        args.State = new FoldableCardComponentState(component.IsFolded);
    }

    private void OnHandleState(EntityUid uid, FoldableCardComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FoldableCardComponentState state)
            return;

        if (state.IsFolded != component.IsFolded)
            SetFolded(uid, component, state.IsFolded);
    }

    private void OnFoldableInit(EntityUid uid, FoldableCardComponent component, ComponentInit args)
    {
        SetFolded(uid, component, component.IsFolded);
    }

    /// <summary>
    /// Returns false if the entity isn't foldable.
    /// </summary>
    public bool IsFolded(EntityUid uid, FoldableCardComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.IsFolded;
    }

    /// <summary>
    /// Set the folded state of the given <see cref="FoldableCardComponent"/>
    /// </summary>
    public void SetFolded(EntityUid uid, FoldableCardComponent component, bool folded)
    {
        if (folded)
        {
            component.CurrentDescription = "card-folded";
        }
        else
        {
            component.CurrentDescription = component.Description;
        }
        component.IsFolded = folded;
        Dirty(uid, component);
        _appearance.SetData(uid, FoldedCardVisuals.State, folded);
    }

    public bool TryToggleFold(EntityUid uid, FoldableCardComponent comp)
    {
        return TrySetFolded(uid, comp, !comp.IsFolded);
    }

    public bool CanToggleFold(EntityUid uid, FoldableCardComponent? fold = null)
    {
        if (!Resolve(uid, ref fold))
            return false;

        var ev = new FoldAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Try to fold/unfold
    /// </summary>
    public bool TrySetFolded(EntityUid uid, FoldableCardComponent comp, bool state)
    {
        if (state == comp.IsFolded)
            return false;

        if (!CanToggleFold(uid, comp))
            return false;

        SetFolded(uid, comp, state);
        return true;
    }

    #region Verb

    private void AddFoldVerb(EntityUid uid, FoldableCardComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!CanToggleFold(uid, component))
            return;

        AlternativeVerb verb = new()
        {
            Act = () => TryToggleFold(uid, component),
            Text = component.IsFolded ? Loc.GetString("unfold-verb") : Loc.GetString("fold-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),

            // If the object is unfolded and they click it, they want to fold it, if it's folded, they want to pick it up
            Priority = component.IsFolded ? 0 : 2,
        };

        args.Verbs.Add(verb);
    }

    #endregion

    [Serializable, NetSerializable]
    public enum FoldedCardVisuals : byte
    {
        State
    }
}

/// <summary>
/// Event raised on an entity to determine if it can be folded.
/// </summary>
/// <param name="Cancelled"></param>
[ByRefEvent]
public record struct FoldAttemptEvent(bool Cancelled = false);
