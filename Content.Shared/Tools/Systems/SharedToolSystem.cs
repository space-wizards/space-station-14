using Content.Shared.DoAfter;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Tools;

public abstract partial class SharedToolSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        InitializeMultipleTool();
        SubscribeLocalEvent<ToolComponent, ToolDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, ToolComponent tool, ToolDoAfterEvent args)
    {
        if (!args.Cancelled)
            PlayToolSound(uid, tool, args.User);

        var ev = args.WrappedEvent;
        ev.DoAfter = args.DoAfter;

        if (args.OriginalTarget != null)
            RaiseLocalEvent(GetEntity(args.OriginalTarget.Value), (object) ev);
        else
            RaiseLocalEvent((object) ev);
    }

    public void PlayToolSound(EntityUid uid, ToolComponent tool, EntityUid? user)
    {
        if (tool.UseSound == null)
            return;

        _audioSystem.PlayPredicted(tool.UseSound, uid, user, tool.UseSound.Params.WithVariation(0.175f).AddVolume(-5f));
    }

    /// <summary>
    ///     Attempts to use a tool on some entity, which will start a DoAfter. Returns true if an interaction occurred.
    ///     Note that this does not mean the interaction was successful, you need to listen for the DoAfter event.
    /// </summary>
    /// <param name="tool">The tool to use</param>
    /// <param name="user">The entity using the tool</param>
    /// <param name="target">The entity that the tool is being used on. This is also the entity that will receive the
    /// event. If null, the event will be broadcast</param>
    /// <param name="doAfterDelay">The base tool use delay (seconds). This will be modified by the tool's quality</param>
    /// <param name="toolQualitiesNeeded">The qualities needed for this tool to work.</param>
    /// <param name="doAfterEv">The event that will be raised when the tool has finished (including cancellation). Event
    /// will be directed at the tool target.</param>
    /// <param name="toolComponent">The tool component.</param>
    /// <returns>Returns true if any interaction takes place.</returns>
    public bool UseTool(
        EntityUid tool,
        EntityUid user,
        EntityUid? target,
        float doAfterDelay,
        IEnumerable<string> toolQualitiesNeeded,
        DoAfterEvent doAfterEv,
        ToolComponent? toolComponent = null)
    {
        return UseTool(tool,
            user,
            target,
            TimeSpan.FromSeconds(doAfterDelay),
            toolQualitiesNeeded,
            doAfterEv,
            out _,
            toolComponent);
    }

    /// <summary>
    ///     Attempts to use a tool on some entity, which will start a DoAfter. Returns true if an interaction occurred.
    ///     Note that this does not mean the interaction was successful, you need to listen for the DoAfter event.
    /// </summary>
    /// <param name="tool">The tool to use</param>
    /// <param name="user">The entity using the tool</param>
    /// <param name="target">The entity that the tool is being used on. This is also the entity that will receive the
    /// event. If null, the event will be broadcast</param>
    /// <param name="delay">The base tool use delay. This will be modified by the tool's quality</param>
    /// <param name="toolQualitiesNeeded">The qualities needed for this tool to work.</param>
    /// <param name="doAfterEv">The event that will be raised when the tool has finished (including cancellation). Event
    /// will be directed at the tool target.</param>
    /// <param name="id">The id of the DoAfter that was created. This may be null even if the function returns true in
    /// the event that this tool-use cancelled an existing DoAfter</param>
    /// <param name="toolComponent">The tool component.</param>
    /// <returns>Returns true if any interaction takes place.</returns>
    public bool UseTool(
        EntityUid tool,
        EntityUid user,
        EntityUid? target,
        TimeSpan delay,
        IEnumerable<string> toolQualitiesNeeded,
        DoAfterEvent doAfterEv,
        out DoAfterId? id,
        ToolComponent? toolComponent = null)
    {
        id = null;
        if (!Resolve(tool, ref toolComponent, false))
            return false;

        if (!CanStartToolUse(tool, user, target, toolQualitiesNeeded, toolComponent))
            return false;

        var toolEvent = new ToolDoAfterEvent(doAfterEv, GetNetEntity(target));
        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay / toolComponent.SpeedModifier, toolEvent, tool, target: target, used: tool)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = tool != user,
            AttemptFrequency = IsWelder(tool) ? AttemptFrequency.EveryTick : AttemptFrequency.Never
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
        return true;
    }

    protected abstract bool IsWelder(EntityUid uid);

    /// <summary>
    ///     Attempts to use a tool on some entity, which will start a DoAfter. Returns true if an interaction occurred.
    ///     Note that this does not mean the interaction was successful, you need to listen for the DoAfter event.
    /// </summary>
    /// <param name="tool">The tool to use</param>
    /// <param name="user">The entity using the tool</param>
    /// <param name="target">The entity that the tool is being used on. This is also the entity that will receive the
    /// event. If null, the event will be broadcast</param>
    /// <param name="doAfterDelay">The base tool use delay (seconds). This will be modified by the tool's quality</param>
    /// <param name="toolQualityNeeded">The quality needed for this tool to work.</param>
    /// <param name="doAfterEv">The event that will be raised when the tool has finished (including cancellation). Event
    /// will be directed at the tool target.</param>
    /// <param name="id">The id of the DoAfter that was created. This may be null even if the function returns true in
    /// the event that this tool-use cancelled an existing DoAfter</param>
    /// <param name="toolComponent">The tool component.</param>
    /// <returns>Returns true if any interaction takes place.</returns>
    public bool UseTool(
        EntityUid tool,
        EntityUid user,
        EntityUid? target,
        float doAfterDelay,
        string toolQualityNeeded,
        DoAfterEvent doAfterEv,
        ToolComponent? toolComponent = null)
    {
        return UseTool(tool,
            user,
            target,
            TimeSpan.FromSeconds(doAfterDelay),
            new[] { toolQualityNeeded },
            doAfterEv,
            out _,
            toolComponent);
    }

    /// <summary>
    ///     Whether a tool entity has the specified quality or not.
    /// </summary>
    public bool HasQuality(EntityUid uid, string quality, ToolComponent? tool = null)
    {
        return Resolve(uid, ref tool, false) && tool.Qualities.Contains(quality);
    }

    /// <summary>
    ///     Whether a tool entity has all specified qualities or not.
    /// </summary>
    public bool HasAllQualities(EntityUid uid, IEnumerable<string> qualities, ToolComponent? tool = null)
    {
        return Resolve(uid, ref tool, false) && tool.Qualities.ContainsAll(qualities);
    }

    private bool CanStartToolUse(EntityUid tool, EntityUid user, EntityUid? target, IEnumerable<string> toolQualitiesNeeded, ToolComponent? toolComponent = null)
    {
        if (!Resolve(tool, ref toolComponent))
            return false;

        // check if the tool can do what's required
        if (!toolComponent.Qualities.ContainsAll(toolQualitiesNeeded))
            return false;

        // check if the user allows using the tool
        var ev = new ToolUserAttemptUseEvent(target);
        RaiseLocalEvent(user, ref ev);
        if (ev.Cancelled)
            return false;

        // check if the tool allows being used
        var beforeAttempt = new ToolUseAttemptEvent(user);
        RaiseLocalEvent(tool, beforeAttempt);
        if (beforeAttempt.Cancelled)
            return false;

        // check if the target allows using the tool
        if (target != null && target != tool)
        {
            RaiseLocalEvent(target.Value, beforeAttempt);
        }

        return !beforeAttempt.Cancelled;
    }

    #region DoAfterEvents

    [Serializable, NetSerializable]
    protected sealed partial class ToolDoAfterEvent : DoAfterEvent
    {
        /// <summary>
        ///     Entity that the wrapped do after event will get directed at. If null, event will be broadcast.
        /// </summary>
        [DataField("target")]
        public NetEntity? OriginalTarget;

        [DataField("wrappedEvent")]
        public DoAfterEvent WrappedEvent = default!;

        private ToolDoAfterEvent()
        {
        }

        public ToolDoAfterEvent(DoAfterEvent wrappedEvent, NetEntity? originalTarget)
        {
            DebugTools.Assert(wrappedEvent.GetType().HasCustomAttribute<NetSerializableAttribute>(), "Tool event is not serializable");

            WrappedEvent = wrappedEvent;
            OriginalTarget = originalTarget;
        }

        public override DoAfterEvent Clone()
        {
            var evClone = WrappedEvent.Clone();

            // Most DoAfter events are immutable
            if (evClone == WrappedEvent)
                return this;

            return new ToolDoAfterEvent(evClone, OriginalTarget);
        }
    }

    [Serializable, NetSerializable]
    protected sealed partial class LatticeCuttingCompleteEvent : DoAfterEvent
    {
        [DataField("coordinates", required:true)]
        public NetCoordinates Coordinates;

        private LatticeCuttingCompleteEvent()
        {
        }

        public LatticeCuttingCompleteEvent(NetCoordinates coordinates)
        {
            Coordinates = coordinates;
        }

        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    protected sealed partial class TilePryingDoAfterEvent : DoAfterEvent
    {
        [DataField("coordinates", required: true)]
        public NetCoordinates Coordinates;

        private TilePryingDoAfterEvent()
        {
        }

        public TilePryingDoAfterEvent(NetCoordinates coordinates)
        {
            Coordinates = coordinates;
        }

        public override DoAfterEvent Clone() => this;
    }
}

[Serializable, NetSerializable]
public sealed partial class CableCuttingFinishedEvent : SimpleDoAfterEvent
{
}

#endregion

