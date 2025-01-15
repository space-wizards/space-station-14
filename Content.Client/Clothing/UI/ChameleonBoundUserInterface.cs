using Content.Client.Clothing.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Tag;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.UI;

[UsedImplicitly]
public sealed class ChameleonBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly ChameleonClothingSystem _chameleon;
    private readonly TagSystem _tag;

    [ViewVariables]
    private ChameleonMenu? _menu;

    public ChameleonBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _chameleon = EntMan.System<ChameleonClothingSystem>();
        _tag = EntMan.System<TagSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChameleonMenu>();
        _menu.OnIdSelected += OnIdSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ChameleonBoundUserInterfaceState st)
            return;

        var targets = _chameleon.GetValidTargets(st.Slot);
        if (st.RequiredTag != null)
        {
            var newTargets = new List<string>();
            foreach (var target in targets)
            {
                if (string.IsNullOrEmpty(target) || !_proto.TryIndex(target, out EntityPrototype? proto))
                    continue;

                if (!proto.TryGetComponent(out TagComponent? tag, _factory) || !_tag.HasTag(tag, st.RequiredTag))
                    continue;

                newTargets.Add(target);
            }
            _menu?.UpdateState(newTargets, st.SelectedId);
        } else
        {
            _menu?.UpdateState(targets, st.SelectedId);
        }
    }

    private void OnIdSelected(string selectedId)
    {
        SendMessage(new ChameleonPrototypeSelectedMessage(selectedId));
    }
}
