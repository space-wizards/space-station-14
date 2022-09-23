using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Paper;
using Content.Server.Hands.Components;
using Content.Server.Station.Systems;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Chemistry Analysis Systems
    /// </summary>
    public sealed class ChemAnalyserSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ChemAnalyserComponent, InteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ChemAnalyserComponent, ChemAnalyserFinishedEvent>(OnChemAnalyserFinished);
        }

        private Queue<EntityUid> AddQueue = new();
        private Queue<EntityUid> RemoveQueue = new();

        /// <summary>
        /// 
        /// </summary>
        public override void Update(float frameTime)
        {
            foreach (var uid in AddQueue)
            {
                EnsureComp<ChemAnalyserRunningComponent>(uid);
            }

            AddQueue.Clear();
            foreach (var uid in RemoveQueue)
            {
                RemComp<ChemAnalyserRunningComponent>(uid);
            }

            RemoveQueue.Clear();

            foreach (var (_, chemAnalyser) in EntityQuery<ChemAnalyserRunningComponent, ChemAnalyserComponent>())
            {
                chemAnalyser.Accumulator += frameTime;

                while (chemAnalyser.Accumulator >= chemAnalyser.Delay)
                {
                    chemAnalyser.Accumulator -= chemAnalyser.Delay;
                    var ev = new ChemAnalyserFinishedEvent(chemAnalyser);
                    RaiseLocalEvent(chemAnalyser.Owner, ev);
                    RemoveQueue.Enqueue(chemAnalyser.Owner);
                }
            }
        }


        /// <summary>
        /// Run the chem analyser machine on use
        /// Checks if the chem analyser accepts the input device based on available tags
        /// Checks the number of solution in the container at the time of analysis (must be above 0 to work)
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, ChemAnalyserComponent component, InteractUsingEvent args)
        {

            var machine = Comp<ChemAnalyserComponent>(uid);

            if (HasComp<ChemAnalyserRunningComponent>(uid))
                return;

            if (TryComp<SolutionContainerManagerComponent>(args.Used, out var solutions))
            {
                bool solutionPresent = false;
                foreach (var solutionContainer in (solutions.Solutions))
                {
                    //Console.WriteLine(solutionContainer.Value.Count); <-- this does not appear to work
                    foreach (var solution in (solutionContainer.Value)) //TODO make this not bad (though it does work)
                    {
                        solutionPresent = true;
                        break;
                    }
                }
                if (!solutionPresent)
                    return;
            }
            else
                return;

            if (component.MachineInputDevice != "") {
                if (TryComp<TagComponent>(args.Used, out var tags))
                {
                    if (!tags.Tags.Contains(component.MachineInputDevice))
                        return;
                }
                else
                    return;
            }

            //_popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid), ("item", args.Used)), uid, Filter.Entities(args.User));

                //check if input is from a solution manager and of the right kind (if specified)
                //if (!HasComp<HandsComponent>(args.User) || HasComp<ToolComponent>(args.Used)) // Don't want to accidentally breach wrenching or whatever
                //    return;

                //check if device requires power or not - if it does then treat as if anchorable

            AddQueue.Enqueue(uid);
            SoundSystem.Play("/Audio/Machines/diagnoser_printing.ogg", Filter.Pvs(uid), uid);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnChemAnalyserFinished(EntityUid uid, ChemAnalyserComponent component, ChemAnalyserFinishedEvent args)
        {

            // spawn a piece of paper.
            var printed = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (!TryComp<PaperComponent>(printed, out var paper))
                return;

            string reportTitle;

            reportTitle = "test";

            FormattedMessage contents = new();
                
            MetaData(printed).EntityName = reportTitle;

            _paperSystem.SetContent(printed, contents.ToMarkup(), paper);
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class ChemAnalyserFinishedEvent : EntityEventArgs
        {
            public ChemAnalyserComponent Machine { get; }
            public ChemAnalyserFinishedEvent(ChemAnalyserComponent machine)
            {
                Machine = machine;
            }
        }
    }
}
