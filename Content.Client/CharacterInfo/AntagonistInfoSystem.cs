using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Robust.Client.UserInterface;

namespace Content.Client.CharacterInfo;

public sealed class AntagonistInfoSystem : EntitySystem
{
    public event Action<AntagonistData>? OnAntagonistUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagonistInfoEvent>(OnAntagonistInfoEvent);
    }

    public void RequestAntagonistInfo(EntityUid? entity)
    {
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestAntagonistInfoEvent(GetNetEntity(entity.Value)));
    }

    private void OnAntagonistInfoEvent(AntagonistInfoEvent msg, EntitySessionEventArgs args)
    {
        var entity = GetEntity(msg.AntagonistNetEntity);
        var data = new AntagonistData(entity, msg.JobTitle, msg.Objectives, Name(entity));

        OnAntagonistUpdate?.Invoke(data);
    }

    public List<Control> GetAntagonistInfoControls(EntityUid uid)
    {
        var ev = new GetAntagonistInfoControlsEvent(uid);
        RaiseLocalEvent(uid, ref ev);
        return ev.Controls;
    }

    public readonly record struct AntagonistData(
        EntityUid Entity,
        string Job,
        Dictionary<string, List<ObjectiveInfo>> Objectives,
        string EntityName
    );

    /// <summary>
    /// Event raised to get additional controls to display in the antagonist info menu.
    /// </summary>
    [ByRefEvent]
    public readonly record struct GetAntagonistInfoControlsEvent(EntityUid Entity)
    {
        public readonly List<Control> Controls = new();

        public readonly EntityUid Entity = Entity;
    }
}
