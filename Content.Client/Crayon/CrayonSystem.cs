using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent, _sharedCharges, _entityManager));
    }

    private sealed class StatusControl : Control
    {
        private readonly Entity<CrayonComponent> _crayon;
        private readonly SharedChargesSystem _chargesSystem;
        private readonly RichTextLabel _label;
        private readonly int _capacity;

        public StatusControl(Entity<CrayonComponent> crayon, SharedChargesSystem chargesSystem, EntityManager entityManage)
        {
            _crayon = crayon;
            _chargesSystem = chargesSystem;
            _capacity = entityManage.GetComponent<LimitedChargesComponent>(_crayon.Owner).MaxCharges;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
                ("color",_crayon.Comp.Color),
                ("state",_crayon.Comp.SelectedState),
                ("charges", _chargesSystem.GetCurrentCharges(_crayon.Owner)),
                ("capacity", _capacity)));
        }
    }
}
