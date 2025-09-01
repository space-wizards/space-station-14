using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Prying.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Systems;

public abstract partial class SharedToolSystem
{
    public void InitializeMultipleTool()
    {
        SubscribeLocalEvent<MultipleToolComponent, ComponentStartup>(OnMultipleToolStartup);
        SubscribeLocalEvent<MultipleToolComponent, ActivateInWorldEvent>(OnMultipleToolActivated);
        SubscribeLocalEvent<MultipleToolComponent, AfterAutoHandleStateEvent>(OnMultipleToolHandleState);
    }

    private void OnMultipleToolHandleState(EntityUid uid, MultipleToolComponent component, ref AfterAutoHandleStateEvent args)
    {
        SetMultipleTool(uid, component);
    }

    private void OnMultipleToolStartup(EntityUid uid, MultipleToolComponent multiple, ComponentStartup args)
    {
        // Only set the multiple tool if we have a tool component.
        if (TryComp(uid, out ToolComponent? tool))
            SetMultipleTool(uid, multiple, tool);
    }

    private void OnMultipleToolActivated(EntityUid uid, MultipleToolComponent multiple, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        //args.Handled =    🌟Starlight🌟 - This doesn't *appear* to ever be relevant, but messes with knife embedding now that they're semi-omnitools
        CycleMultipleTool(uid, multiple, args.User);
    }

    public bool CycleMultipleTool(EntityUid uid, MultipleToolComponent? multiple = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref multiple))
            return false;

        if (multiple.Entries.Length == 0)
            return false;

        multiple.CurrentEntry = (uint)((multiple.CurrentEntry + 1) % multiple.Entries.Length);
        SetMultipleTool(uid, multiple, playSound: true, user: user);

        return true;
    }

    public virtual void SetMultipleTool(EntityUid uid,
        MultipleToolComponent? multiple = null,
        ToolComponent? tool = null,
        bool playSound = false,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref multiple, ref tool))
            return;

        Dirty(uid, multiple);

        if (multiple.Entries.Length <= multiple.CurrentEntry)
        {
            multiple.CurrentQualityName = Loc.GetString("multiple-tool-component-no-behavior");
            return;
        }

        var current = multiple.Entries[multiple.CurrentEntry];
        tool.UseSound = current.UseSound;
        tool.Qualities = current.Behavior;
        tool.SpeedModifier = current.SpeedModifier; /// Starlight

        // TODO: Replace this with a better solution later
        if (TryComp<PryingComponent>(uid, out var pryComp))
        {
            pryComp.Enabled = current.Behavior.Contains("Prying");
        }

        /// STARLIGHT Start
        /// Fix to set welder component state when said state is not in use.
        /// Prevents ToolSystem.Welder from screwing with other tools
        if (TryComp<WelderComponent>(uid, out var weldComp))
        {
            if (multiple.CurrentQualityName.ToLower().Contains("welding"))
            {
                weldComp.ComponentActive = true;
            }
            else
            {
                weldComp.ComponentActive = false;
            }
        }
        // STARLIGHT End

        if (playSound && current.ChangeSound != null)
            _audioSystem.PlayPredicted(current.ChangeSound, uid, user);

        if (_protoMan.TryIndex(current.Behavior.First(), out ToolQualityPrototype? quality))
            multiple.CurrentQualityName = Loc.GetString(quality.Name);
    }
}

