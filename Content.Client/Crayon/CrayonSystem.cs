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
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent, _charges, _entityManager));
    }

    private sealed class StatusControl : Control
    {
        private readonly Entity<CrayonComponent> _crayon;
        private readonly SharedChargesSystem _charges;
        private readonly RichTextLabel _label;
        private readonly int _capacity;

        public StatusControl(Entity<CrayonComponent> crayon, SharedChargesSystem charges, EntityManager entityManage)
        {
            _crayon = crayon;
            _charges = charges;
            _capacity = entityManage.GetComponent<LimitedChargesComponent>(_crayon.Owner).MaxCharges;
            _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
                ("color",_crayon.Comp.Color),
                ("state",_crayon.Comp.SelectedState),
                ("charges", _charges.GetCurrentCharges(_crayon.Owner)),
                ("capacity", _capacity)));
        }
    }
}
