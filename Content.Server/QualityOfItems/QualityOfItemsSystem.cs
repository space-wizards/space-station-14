using Content.Shared.Examine;
using Content.Shared.QualityOfItem;

namespace Content.Server.QualityOfItem
{
    public sealed class QualityOfItemsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<QualityOfItemComponent, ExaminedEvent>(OnExamineItem);
        }

        public void OnExamineItem(EntityUid uid, QualityOfItemComponent comp, ExaminedEvent args)
        {
            if(comp.Quality > 6 || comp.Quality < 0)
                return;
            string str = "quality-of-item-" + comp.Quality.ToString();

            args.PushMarkup(Loc.GetString(str));
        }
    }
}
