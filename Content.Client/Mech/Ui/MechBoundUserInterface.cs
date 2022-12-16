using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly EntityUid _mech;

    private MechMenu? _menu;

    public MechBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _mech = owner.Owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(_mech);

        _menu.OnClose += Close;
        _menu.OpenCenteredLeft();

        _menu.OnRemoveButtonPressed += uid =>
        {
            SendMessage(new MechEquipmentRemoveMessage(uid));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MechBoundUiState msg)
            return;
        UpdateEquipmentControls(msg);
        _menu?.UpdateMechStats();
        _menu?.UpdateEquipmentView();
    }

    public void UpdateEquipmentControls(MechBoundUiState state)
    {
        if (!_ent.TryGetComponent<MechComponent>(_mech, out var mechComp))
            return;

        foreach (var ent in mechComp.EquipmentContainer.ContainedEntities)
        {
            var ui = GetEquipmentUi(ent);
            if (ui == null)
                continue;
            foreach (var (attached, estate) in state.EquipmentStates)
            {
                if (ent == attached)
                    ui.UpdateState(estate);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Close();
    }

    public UIFragment? GetEquipmentUi(EntityUid? uid)
    {
        var component = _ent.GetComponentOrNull<UIFragmentComponent>(uid);
        component?.Ui?.Setup(this, uid);
        return component?.Ui;
    }
}

