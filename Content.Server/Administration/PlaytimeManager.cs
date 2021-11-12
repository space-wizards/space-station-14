using System;
using System.Collections.Generic;
using Content.Server.Database;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Administration;

/// <summary>
///     Tracks a player's <see cref="TotalPlaytime"/> and <see cref="LivingPlaytime"/>, updates every <see cref="UpdateInterval"/> seconds and on roundend.
/// </summary>
public interface IPlaytimeManager
{
    void Update(float deltaTime);

    void Shutdown();

    public void FetchPlaytime(IPlayerSession session, out int livingTime, out int totalTime);

    float SecondsSinceUpdate { get; set; }

    float UpdateInterval { get; set; }

    bool Ticking { get; set; }

    public void TryLoadPlaytime(IPlayerSession session, PlayerData data);
}

public class PlaytimeManager : IPlaytimeManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    /// <summary>
    ///     Tracks time between updates. See <see cref="UpdateInterval"/>
    /// </summary>
    public float SecondsSinceUpdate { get; set; } = 0f;

    /// <summary>
    ///     Number of seconds between each <see cref="UpdatePlaytimes"/>. Defaults to 10 minutes.
    /// </summary>
    public float UpdateInterval { get; set; } = 600f;

    /// <summary>
    ///     Whether playtime tracking is active
    /// </summary>
    public bool Ticking { get; set; } = true;

    public void Update(float deltaTime)
    {
        if (!Ticking) return;

        SecondsSinceUpdate += deltaTime;

        if (SecondsSinceUpdate >= UpdateInterval)
        {
            UpdatePlaytimes();
            SecondsSinceUpdate = 0;
        }
    }

    /// <summary>
    ///     Called during round end to ensure playtime isn't lost.
    /// </summary>
    public void Shutdown()
    {
        Ticking = false;
        UpdateInterval = Math.Max(UpdateInterval -= SecondsSinceUpdate, 0);
        UpdatePlaytimes();
    }

    /// <summary>
    ///     Returns the living and total playtime for a <see cref="IPlayerSession"/> in minutes.
    /// </summary>
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

    /// <summary>
    ///     Handles actual playtime updates in the <see cref="PlayerData"/> and database.
    /// </summary>
    private void UpdatePlaytimes()
    {
        var intervalSpan = TimeSpan.FromSeconds(UpdateInterval);

        foreach (IPlayerSession session in _playerManager.GetAllPlayers())
        {
            var data = session.ContentData();
            if (data is null) continue;

            switch (session.Status)
            {
                // Don't let them lose time between last update and disconnect
                case SessionStatus.Disconnected:
                {
                    var difference = _gameTiming.RealTime - data.DisconnectTime;
                    // Checks to make sure we only update playtime once after DC
                    if (difference is not null && difference.Value < intervalSpan)
                    {
                        AddTime(session, difference.Value);
                    }
                    else continue;
                    break;
                }
                case SessionStatus.Connected:
                case SessionStatus.InGame:
                {
                    var difference = _gameTiming.RealTime - data.JoinTime;
                    //They joined since the last update and only get partial time
                    if (difference < intervalSpan)
                    {
                        AddTime(session, difference);
                    }
                    //They get the full interval
                    else
                    {
                        AddTime(session, intervalSpan);
                    }

                    break;
                }

            }

            _dbManager.SavePlaytimeAsync(session.UserId, data.TotalPlaytime, data.LivingPlaytime);
        }
    }

    /// <summary>
    ///     Adds playtime in minutes to a session's <see cref="PlayerData"/>
    /// </summary>
    private void AddTime(IPlayerSession session, TimeSpan time)
    {
        var data = session.ContentData();
        if (data is null) return;

        data.TotalPlaytime += time.Minutes;
        if (!(data.Mind?.CharacterDeadIC ?? true))
        {
            data.LivingPlaytime += time.Minutes;
        }
    }

    /// <summary>
    ///     Loads a session's playtime from the database and stores it in <see cref="PlayerData"/>
    /// </summary>
    public async void TryLoadPlaytime(IPlayerSession session, PlayerData data)
    {
        var record = await _dbManager.GetPlayerRecordByUserId(session.UserId);
        if (record is null) return;
        data.TotalPlaytime = record.TotalPlaytime;
        data.LivingPlaytime = record.LivingPlaytime;
    }
}
