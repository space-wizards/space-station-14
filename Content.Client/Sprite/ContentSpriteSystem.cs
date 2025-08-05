using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Client.Administration.Managers;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Sprite;

public sealed class ContentSpriteSystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceManager _resManager = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

    private ContentSpriteControl _control = new();

    public static readonly ResPath Exports = new ResPath("/Exports");

    public override void Initialize()
    {
        base.Initialize();

        _resManager.UserData.CreateDir(Exports);
        _ui.RootControl.AddChild(_control);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var queued in _control.QueuedTextures)
        {
            queued.Tcs.SetCanceled();
        }

        _control.QueuedTextures.Clear();

        _ui.RootControl.RemoveChild(_control);
    }

    /// <summary>
    /// Exports sprites for all directions
    /// </summary>
    public async Task Export(EntityUid entity, bool includeId = true, CancellationToken cancelToken = default)
    {
        var tasks = new Task[4];
        var i = 0;

        foreach (var dir in new Direction[]
                 {
                     Direction.South,
                     Direction.East,
                     Direction.North,
                     Direction.West,
                 })
        {
            tasks[i++] = Export(entity, dir, includeId: includeId, cancelToken);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Exports the sprite for a particular direction.
    /// </summary>
    public async Task Export(EntityUid entity, Direction direction, bool includeId = true, CancellationToken cancelToken = default)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp(entity, out SpriteComponent? spriteComp))
            return;

        // Don't want to wait for engine pr
        var size = Vector2i.Zero;

        foreach (var layer in spriteComp.AllLayers)
        {
            if (!layer.Visible)
                continue;

            size = Vector2i.ComponentMax(size, layer.PixelSize);
        }

        // Stop asserts
        if (size.Equals(Vector2i.Zero))
            return;

        var texture = _clyde.CreateRenderTarget(new Vector2i(size.X, size.Y), new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "export");
        var tcs = new TaskCompletionSource(cancelToken);

        _control.QueuedTextures.Enqueue((texture, direction, entity, includeId, tcs));

        await tcs.Task;
    }

    private void GetVerbs(GetVerbsEvent<Verb> ev)
    {
        if (!_adminManager.IsAdmin())
            return;

        var target = ev.Target;
        Verb verb = new()
        {
            Text = Loc.GetString("export-entity-verb-get-data-text"),
            Category = VerbCategory.Debug,
            Act = async () =>
            {
                try
                {
                    await Export(target);
                }
                catch (Exception e)
                {
                    _runtimeLog.LogException(e, $"{nameof(ContentSpriteSystem)}.{nameof(Export)}");
                }
            },
        };

        ev.Verbs.Add(verb);
    }

    /// <summary>
    /// This is horrible. I asked PJB if there's an easy way to render straight to a texture outside of the render loop
    /// and she also mentioned this as a bad possibility.
    /// </summary>
    private sealed class ContentSpriteControl : Control
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly ILogManager _logMan = default!;
        [Dependency] private readonly IResourceManager _resManager = default!;

        internal Queue<(
            IRenderTexture Texture,
            Direction Direction,
            EntityUid Entity,
            bool IncludeId,
            TaskCompletionSource Tcs)> QueuedTextures = new();

        private ISawmill _sawmill;

        public ContentSpriteControl()
        {
            IoCManager.InjectDependencies(this);
            _sawmill = _logMan.GetSawmill("sprite.export");
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            while (QueuedTextures.TryDequeue(out var queued))
            {
                if (queued.Tcs.Task.IsCanceled)
                    continue;

                try
                {
                    if (!_entManager.TryGetComponent(queued.Entity, out MetaDataComponent? metadata))
                        continue;

                    var filename = metadata.EntityName;
                    var result = queued;

                    handle.RenderInRenderTarget(queued.Texture, () =>
                    {
                        handle.DrawEntity(result.Entity, result.Texture.Size / 2, Vector2.One, Angle.Zero,
                            overrideDirection: result.Direction);
                    }, Color.Transparent);

                    ResPath fullFileName;

                    if (queued.IncludeId)
                    {
                        fullFileName = Exports / $"{filename}-{queued.Direction}-{queued.Entity}.png";
                    }
                    else
                    {
                        fullFileName = Exports / $"{filename}-{queued.Direction}.png";
                    }

                    queued.Texture.CopyPixelsToMemory<Rgba32>(image =>
                    {
                        if (_resManager.UserData.Exists(fullFileName))
                        {
                            _sawmill.Info($"Found existing file {fullFileName} to replace.");
                            _resManager.UserData.Delete(fullFileName);
                        }

                        using var file =
                            _resManager.UserData.Open(fullFileName, FileMode.CreateNew, FileAccess.Write,
                                FileShare.None);

                        image.SaveAsPng(file);
                    });

                    _sawmill.Info($"Saved screenshot to {fullFileName}");
                    queued.Tcs.SetResult();
                }
                catch (Exception exc)
                {
                    queued.Texture.Dispose();

                    if (!string.IsNullOrEmpty(exc.StackTrace))
                        _sawmill.Fatal(exc.StackTrace);

                    queued.Tcs.SetException(exc);
                }
            }
        }
    }
}
