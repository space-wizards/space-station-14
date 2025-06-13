using Content.Client.Administration.Managers;
using Content.Client.Interactable.Components;
using Content.Shared.Ghost;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Stealth;

public sealed class StealthSystem : SharedStealthSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!; // imp
    [Dependency] private readonly IClientAdminManager _adminManager = default!; // imp
    [Dependency] private readonly ContainerSystem _containerSystem = default!; // imp

    private ShaderInstance _shader = default!;
    private ShaderInstance _altShader = default!;

    private float timer = 0;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index<ShaderPrototype>("Stealth").InstanceUnique();
        _altShader = _protoMan.Index<ShaderPrototype>("AccessibleFullStealth").InstanceUnique();

        SubscribeLocalEvent<StealthComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StealthComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StealthComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    //no longer needs a force update! yaaaaay!

    public override void SetEnabled(EntityUid uid, bool value, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.SetEnabled(uid, value, component);
        SetShader(uid, value, component);
    }

    private void SetShader(EntityUid uid, bool enabled, StealthComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref sprite, false))
            return;

        _sprite.SetColor((uid, sprite), Color.White);
        //imp special - use the alternative full-invis shader if we're set to
        var shaderToUse = component.UseAltShader ? _altShader : _shader;
        sprite.PostShader = enabled ? shaderToUse : null;
        //imp special end
        sprite.GetScreenTexture = enabled;
        sprite.RaiseShaderEvent = enabled;

        if (!enabled)
        {
            if (component.HadOutline && !TerminatingOrDeleted(uid))
                EnsureComp<InteractionOutlineComponent>(uid);
            return;
        }

        if (TryComp(uid, out InteractionOutlineComponent? outline))
        {
            RemCompDeferred(uid, outline);
            component.HadOutline = true;
        }
    }

    private void OnStartup(EntityUid uid, StealthComponent component, ComponentStartup args)
    {
        SetShader(uid, component.Enabled, component);
    }

    private void OnShutdown(EntityUid uid, StealthComponent component, ComponentShutdown args)
    {
        if (!Terminating(uid))
            SetShader(uid, false, component);
    }

    private void OnShaderRender(EntityUid uid, StealthComponent component, BeforePostShaderRenderEvent args)
    {
        //imp special - show an outline for people that should see it, goes along with complete invisibility
        //includes the entity with the component, any admins & any ghosts
        var shaderToUse = component.UseAltShader ? _altShader : _shader;
        shaderToUse.SetParameter("ShowOutline", false); //make sure it's always false by default

        bool isCorrectSession = false;
        bool isGhost = false;
        bool isInContainer = false;

        if (_playerManager.LocalSession != null)
        {
            if (_playerManager.TryGetSessionByEntity(uid, out var playerSession))
            {
                isCorrectSession = playerSession.UserId == _playerManager.LocalSession.UserId;
            }

            isGhost = HasComp<GhostComponent>(_playerManager.LocalSession.AttachedEntity);

            if (_playerManager.LocalSession.AttachedEntity is { } entity) //why can you not just use a normal nullcheck for this I hate c#
            {
                isInContainer = _containerSystem.ContainsEntity(uid, entity);
            }
        }

        if (isCorrectSession || isGhost || isInContainer)
        {
            shaderToUse.SetParameter("ShowOutline", true);
        }
        //imp special end

        // Distortion effect uses screen coordinates. If a player moves, the entities appear to move on screen. this
        // makes the distortion very noticeable.

        // So we need to use relative screen coordinates. The reference frame we use is the parent's position on screen.
        // this ensures that if the Stealth is not moving relative to the parent, its relative screen position remains
        // unchanged.
        var parent = Transform(uid).ParentUid;
        if (!parent.IsValid())
            return; // should never happen, but lets not kill the client.
        var parentXform = Transform(parent);
        var reference = args.Viewport.WorldToLocal(_transformSystem.GetWorldPosition(parentXform));
        reference.X = -reference.X;
        var visibility = GetVisibility(uid, component);

        // actual visual visibility effect is limited to +/- 1.
        visibility = Math.Clamp(visibility, -1f, 1f);

        shaderToUse.SetParameter("reference", reference);
        shaderToUse.SetParameter("visibility", visibility);

        visibility = MathF.Max(0, visibility);
        _sprite.SetColor((uid, args.Sprite), new Color(visibility, visibility, 1, 1));
    }
}
