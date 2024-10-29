using Content.Shared.IdentityManagement;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared.Tools.EntitySystems;

public sealed class ToolOpenableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToolOpenableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ToolOpenableComponent, ToolOpenableDoAfterEventToggleOpen>(OnOpenableStateToggled);
        SubscribeLocalEvent<ToolOpenableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ToolOpenableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ToolOpenableComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);
    }

    private void OnInit(Entity<ToolOpenableComponent> entity, ref ComponentInit args)
    {
        UpdateAppearance(entity);
        Dirty(entity);
    }

    private void OnInteractUsing(Entity<ToolOpenableComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryOpenClose(entity, args.Used, args.User))
            args.Handled = true;
    }

    /// <summary>
    ///     Try to open or close what is openable.
    /// </summary>
    /// <returns> Returns false if you can't interact with the openable thing with the given item. </returns>
    private bool TryOpenClose(Entity<ToolOpenableComponent> entity, EntityUid? toolToToggle, EntityUid user)
    {
        var neededToolQuantity = entity.Comp.IsOpen ? entity.Comp.CloseToolQualityNeeded : entity.Comp.OpenToolQualityNeeded;
        var time = entity.Comp.IsOpen ? entity.Comp.CloseTime : entity.Comp.OpenTime;
        var evt = new ToolOpenableDoAfterEventToggleOpen();

        // If neededToolQuantity is null it can only be open be opened with the verbs.
        if (toolToToggle == null || neededToolQuantity == null)
            return false;

        return _tool.UseTool(toolToToggle.Value, user, entity, time, neededToolQuantity, evt);
    }

    private void OnOpenableStateToggled(Entity<ToolOpenableComponent> entity, ref ToolOpenableDoAfterEventToggleOpen args)
    {
        if (args.Cancelled)
            return;

        ToggleState(entity);
    }

    /// <summary>
    ///     Toggle the state and update appearance.
    /// </summary>
    private void ToggleState(Entity<ToolOpenableComponent> entity)
    {
        entity.Comp.IsOpen = !entity.Comp.IsOpen;
        UpdateAppearance(entity);
        Dirty(entity);
    }

    #region Helper functions

    private string GetName(Entity<ToolOpenableComponent> entity)
    {
        if (entity.Comp.Name == null)
            return Identity.Name(entity, EntityManager);
        return Loc.GetString(entity.Comp.Name);
    }

    public bool IsOpen(EntityUid uid, ToolOpenableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return true;

        return component.IsOpen;
    }

    private void UpdateAppearance(Entity<ToolOpenableComponent> entity)
    {
        _appearance.SetData(entity, ToolOpenableVisuals.ToolOpenableVisualState, entity.Comp.IsOpen ? ToolOpenableVisualState.Open : ToolOpenableVisualState.Closed);
    }

    #endregion

    #region User interface functions

    private void OnExamine(Entity<ToolOpenableComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string msg;
        var name = GetName(entity);
        if (entity.Comp.IsOpen)
            msg = Loc.GetString("tool-openable-component-examine-opened", ("name", name));
        else
            msg = Loc.GetString("tool-openable-component-examine-closed", ("name", name));

        args.PushMarkup(msg);
    }

    private void OnGetVerb(Entity<ToolOpenableComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !entity.Comp.HasVerbs)
            return;

        var user = args.User;
        var item = args.Using;
        var name = GetName(entity);

        var toggleVerb = new InteractionVerb
        {
            IconEntity = GetNetEntity(item)
        };

        if (entity.Comp.IsOpen)
        {
            toggleVerb.Text = toggleVerb.Message = Loc.GetString("tool-openable-component-verb-close");
            var neededQual = entity.Comp.CloseToolQualityNeeded;

            // If neededQual is null you don't need a tool to open / close.
            if (neededQual != null &&
                (item == null || !_tool.HasQuality(item.Value, neededQual)))
            {
                toggleVerb.Disabled = true;
                toggleVerb.Message = Loc.GetString("tool-openable-component-verb-cant-close", ("name", name));
            }

            if (neededQual == null)
                toggleVerb.Act = () => ToggleState(entity);
            else
                toggleVerb.Act = () => TryOpenClose(entity, item, user);

            args.Verbs.Add(toggleVerb);
        }
        else
        {
            // The open verb should only appear when holding the correct tool or if no tool is needed.

            toggleVerb.Text = toggleVerb.Message = Loc.GetString("tool-openable-component-verb-open");
            var neededQual = entity.Comp.OpenToolQualityNeeded;

            if (neededQual == null)
            {
                toggleVerb.Act = () => ToggleState(entity);
                args.Verbs.Add(toggleVerb);
            }
            else if (item != null && _tool.HasQuality(item.Value, neededQual))
            {
                toggleVerb.Act = () => TryOpenClose(entity, item, user);
                args.Verbs.Add(toggleVerb);
            }
        }
    }

    #endregion
}
