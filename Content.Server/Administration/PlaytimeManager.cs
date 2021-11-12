using System;
using System.Collections.Generic;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Content.Server.Administration;

public interface IPlaytimeManager
{
    void Initialize();

    void Update(float deltaTime);

    //void Shutdown();

    public void FetchPlaytime(IPlayerSession session, out int livingTime, out int totalTime);

    float SecondsSinceUpdate { get; set; }

    float UpdateInterval { get; set; }

}

public class PlaytimeManager : IPlaytimeManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public float SecondsSinceUpdate { get; set; } = 0f;

    public float UpdateInterval { get; set; } = 600f;

    public void Initialize()
    {
    }

    public void Update(float deltaTime)
    {
        SecondsSinceUpdate += deltaTime;

        if (SecondsSinceUpdate >= UpdateInterval)
        {
            UpdatePlaytimes();
        }
    }

    public void Shutdown()
    {

    }


    public void FetchPlaytime(IPlayerSession session, out int livingTime, out int totalTime)
    {
        var data = session.ContentData();
        if (data is null)
        {
            livingTime = 0;
            totalTime = 0;
            return;
        }
        livingTime = data.LivingPlaytime;
        totalTime = data.TotalPlaytime;
    }

    private void UpdatePlaytimes()
    {
        foreach (IPlayerSession session in _playerManager.GetAllPlayers())
        {
            if (session.Status != SessionStatus.Connected || session.Status != SessionStatus.InGame) continue;


        }
    }
}
