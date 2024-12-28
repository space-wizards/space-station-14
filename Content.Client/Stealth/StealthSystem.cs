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
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index<ShaderPrototype>("AccessibleFullStealth").InstanceUnique();

        SubscribeLocalEvent<StealthComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StealthComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StealthComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    public override void SetEnabled(EntityUid uid, bool value, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Enabled == value)
            return;

        base.SetEnabled(uid, value, component);
        SetShader(uid, value, component);
    }

    private void SetShader(EntityUid uid, bool enabled, StealthComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref sprite, false))
            return;

        sprite.Color = Color.White;
        sprite.PostShader = enabled ? _shader : null;
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

        //imp special - show an outline for people that should see it, goes along with complete invisibility
        //includes the entity with the component, any admins & any ghosts
        //todo want this to check for if the player's entity is inside a container as well
        _shader.SetParameter("ShowOutline", false); //make sure it's always false by default

        bool isAdmin = false;
        bool isCorrectSession = false;
        bool isGhost = false;
        bool isInContainer = false;

        if (_playerManager.LocalSession != null)
        {
            if (_playerManager.TryGetSessionByEntity(uid, out var playerSession))
            {
                isCorrectSession = playerSession.UserId == _playerManager.LocalSession.UserId;
            }

            isAdmin = _adminManager.IsAdmin();
            isGhost = HasComp<GhostComponent>(_playerManager.LocalSession.AttachedEntity);
        }

        if (isAdmin || isCorrectSession || isGhost || isInContainer)
        {
            _shader.SetParameter("ShowOutline", true);
        }
        //imp special end

        // actual visual visibility effect is limited to +/- 1.
        visibility = Math.Clamp(visibility, -1f, 1f);

        _shader.SetParameter("reference", reference);
        _shader.SetParameter("visibility", visibility);

        visibility = MathF.Max(0, visibility);
        args.Sprite.Color = new Color(visibility, visibility, 1, 1);
    }
}
