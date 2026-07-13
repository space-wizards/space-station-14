using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Signature;
using Content.Shared.Verbs;

namespace Content.Shared.Pen;

public sealed partial class PenSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public readonly Dictionary<int, LocId> PenBrushWriteNames = new()
    {
        [1] = "pen-brush-write-normal",
        [2] = "pen-brush-write-medium",
        [4] = "pen-brush-write-large",
    };

    public readonly Dictionary<int, LocId> PenBrushEraseNames = new()
    {
        [2] = "pen-brush-erase-normal",
        [4] = "pen-brush-erase-medium",
        [6] = "pen-brush-erase-large",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<PenComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<PaperComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnGetVerbs(Entity<PenComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract)
            return;

        args.Verbs.UnionWith(CreateVerb(ent, args.User));
    }

    private void OnUIOpened(Entity<PaperComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent, args.Actor);
    }

    private List<Verb> CreateVerb(Entity<PenComponent> ent, EntityUid user)
    {
        List<Verb> verbs = [];

        foreach (var writeSize in PenBrushWriteNames)
        {
            var writeVerb = new Verb
            {
                Text = Loc.GetString(writeSize.Value),
                Disabled = ent.Comp.BrushWriteSize == writeSize.Key,
                Act = () =>
                {
                    ent.Comp.BrushWriteSize = writeSize.Key;
                    Dirty(ent);

                    UpdateUI(ent, user);
                },
                Priority = -writeSize.Key,
                Category = VerbCategory.PenWriteSize,
            };

            verbs.Add(writeVerb);
        }

        foreach (var eraseSize in PenBrushEraseNames)
        {
            var eraseVerb = new Verb
            {
                Text = Loc.GetString(eraseSize.Value),
                Disabled = ent.Comp.BrushEraseSize == eraseSize.Key,
                Act = () =>
                {
                    ent.Comp.BrushEraseSize = eraseSize.Key;
                    Dirty(ent);

                    UpdateUI(ent, user);
                },
                Priority = -eraseSize.Key,
                Category = VerbCategory.PenEraseSize,
            };

            verbs.Add(eraseVerb);
        }

        return verbs;
    }

    private void UpdateUI(Entity<PenComponent> ent, EntityUid user)
    {
        var enumerable = _ui.GetActorUis(user);

        foreach (var ui in enumerable)
        {
            if (ui.Key is not PaperComponent.PaperUiKey.Key)
                continue;

            var state = new UpdatePenBrushPaperState(ent.Comp.BrushWriteSize, ent.Comp.BrushEraseSize);
            _ui.SetUiState(ui.Entity, ui.Key, state);
        }
    }

    private void UpdateUI(Entity<PaperComponent> ent, EntityUid user)
    {
        Entity<PenComponent>? pen = null;

        var enumerate = _hands.EnumerateHeld(user);

        foreach (var handItem in enumerate)
        {
            if (!TryComp<PenComponent>(handItem, out var penComp))
                continue;

            pen = (handItem, penComp);
            break;
        }

        if (pen == null)
            return;

        var enumerable = _ui.GetActorUis(user);

        foreach (var ui in enumerable)
        {
            if (ui.Key is not PaperComponent.PaperUiKey.Key)
                continue;

            var state = new UpdatePenBrushPaperState(pen.Value.Comp.BrushWriteSize, pen.Value.Comp.BrushEraseSize);
            _ui.SetUiState(ent.Owner, ui.Key, state);
        }
    }
}
