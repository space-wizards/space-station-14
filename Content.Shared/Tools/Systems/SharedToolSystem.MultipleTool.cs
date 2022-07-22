using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tools;

public abstract class SharedToolSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SharedMultipleToolComponent, ComponentStartup>(OnMultipleToolStartup);
        SubscribeLocalEvent<SharedMultipleToolComponent, ActivateInWorldEvent>(OnMultipleToolActivated);
        SubscribeLocalEvent<SharedMultipleToolComponent, ComponentGetState>(OnMultipleToolGetState);
        SubscribeLocalEvent<SharedMultipleToolComponent, ComponentHandleState>(OnMultipleToolHandleState);
    }

    private void OnMultipleToolHandleState(EntityUid uid, SharedMultipleToolComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MultipleToolComponentState state)
            return;

        component.CurrentEntry = state.Selected;
        SetMultipleTool(uid, component);
    }

    private void OnMultipleToolStartup(EntityUid uid, SharedMultipleToolComponent multiple, ComponentStartup args)
    {
        // Only set the multiple tool if we have a tool component.
        if(EntityManager.TryGetComponent(uid, out ToolComponent? tool))
            SetMultipleTool(uid, multiple, tool);
    }

    private void OnMultipleToolActivated(EntityUid uid, SharedMultipleToolComponent multiple, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = CycleMultipleTool(uid, multiple);
    }

    private void OnMultipleToolGetState(EntityUid uid, SharedMultipleToolComponent multiple, ref ComponentGetState args)
    {
        args.State = new MultipleToolComponentState(multiple.CurrentEntry);
    }

    public bool CycleMultipleTool(EntityUid uid, SharedMultipleToolComponent? multiple = null)
    {
        if (!Resolve(uid, ref multiple))
            return false;

        if (multiple.Entries.Length == 0)
            return false;

        multiple.CurrentEntry = (uint) ((multiple.CurrentEntry + 1) % multiple.Entries.Length);
        SetMultipleTool(uid, multiple);

        var current = multiple.Entries[multiple.CurrentEntry];

        if(current.ChangeSound is {} changeSound)
            SoundSystem.Play(changeSound.GetSound(), Filter.Pvs(uid), uid);

        return true;
    }

    public virtual void SetMultipleTool(EntityUid uid, SharedMultipleToolComponent? multiple = null, ToolComponent? tool = null)
    {
        if (!Resolve(uid, ref multiple, ref tool))
            return;

        if (multiple.Entries.Length <= multiple.CurrentEntry)
        {
            Loc.GetString("multiple-tool-component-no-behavior");
            return;
        }

        var current = multiple.Entries[multiple.CurrentEntry];
        tool.UseSound = current.Sound;
        tool.Qualities = current.Behavior;

        if (_protoMan.TryIndex(current.Behavior.First(), out ToolQualityPrototype? quality))
        {
            multiple.CurrentQualityName = Loc.GetString(quality.Name);
        }

        Dirty(multiple);
    }
}

