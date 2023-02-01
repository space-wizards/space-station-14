using System.Linq;
using Robust.Shared.GameStates;
using JetBrains.Annotations;

namespace Content.Shared.Borgs
{
    public class SharedLawsSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<LawsComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<LawsComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<LawsComponent, ComponentInit>(OnInit);
        }
        private void OnGetState(EntityUid uid, LawsComponent component, ref ComponentGetState args)
        {
            args.State = new LawsComponentState((new Dictionary<int, (string Text, LawProperties Properties)>(component.Laws)));
        }

        private void OnHandleState(EntityUid uid, LawsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not LawsComponentState cast)
                return;

            component.Laws = new SortedDictionary<int, (string Text, LawProperties Properties)>(cast.Laws);
        }

        private void OnInit(EntityUid uid, LawsComponent component, ComponentInit args)
        {
            int i = 1;
            foreach (var law in component.InitialLaws)
            {
                TryAddLaw(uid, law, i, component: component);
            }
        }

        [PublicAPI]
        public void ClearLaws(EntityUid uid, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            component.Laws.Clear();
            Dirty(component);
        }

        public bool TryAddLaw(EntityUid uid, string law, int? index = null, LawProperties properties = LawProperties.Default, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            if (index == null)
                index = component.Laws.Keys.Last() + 1;

            if (component.Laws.ContainsKey((int) index))
            {
                Logger.Error("Cannot add law. Laws component on " + ToPrettyString(uid) + " already has a law at index " + index);
                return false;
            }

            component.Laws.Add((int) index, (law, properties));
            Dirty(component);
            return true;
        }
        /// <summary>
        /// Remove the highest indexed value of a list, or a specified index.
        /// </summary>
        public bool TryRemoveLaw(EntityUid uid, int? index = null, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            if (component.Laws.Count == 0)
                return false;

            if (index == null)
                index = component.Laws.Keys.Last();

            if (!component.Laws.ContainsKey((int) index))
                return false;

            component.Laws.Remove((int) index);
            Dirty(component);
            return true;
        }
    }
}
