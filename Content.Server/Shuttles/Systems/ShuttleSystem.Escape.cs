using Content.Server.GameTicking.Events;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
   /*
    * Handles the escape shuttle + Centcomm
    */

   [Dependency] private readonly IMapLoader _loader = default!;

   private MapId? _centcommMap;
   private EntityUid? _centcomm;
   private EntityUid? _escapeShuttle;

   // TODO: Use uhhhhhhhhh prototypes I guess?

   /*
    * TODO: When shuttle call < 30 seconds block recalls
    * TODO: When call happened issue event, that's when you start queueing hyperspace from Centcomm and activate Centcomm
    * TODO: After n time unlock all controls, maybe put it on the shuttle state? Ask slorkitos
    */

   private void InitializeEscape()
   {
       SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
   }

   private void OnRoundStart(RoundStartingEvent ev)
   {
       Setup();
   }

   /// <summary>
   /// Spawns the escape shuttle for each station and starts the countdown until controls unlock.
   /// </summary>
   public void CallEscapeShuttle()
   {
       // TODO: Need a console that allows you to authorise early launch.

       // TODO: When shuttle launches set a timer for round end.
   }

   private void Setup()
   {
       DebugTools.Assert(_centcommMap == null);
       _centcommMap = _mapManager.CreateMap();
       _mapManager.SetMapPaused(_centcommMap.Value, true);

       // Load Centcomm
       var (_, centcomm) = _loader.LoadBlueprint(_centcommMap.Value, "/Maps/Salvage/stationstation.yml", new MapLoadOptions());
       _centcomm = centcomm;

       // Load escape shuttle
       var (_, shuttle) = _loader.LoadBlueprint(_centcommMap.Value, "/Maps/cargo_shuttle.yml", new MapLoadOptions()
       {
           // Should be far enough... right? I'm too lazy to bounds check centcomm.
           Offset = Vector2.One * 1000f,
       });
       _escapeShuttle = shuttle;
   }

   private void CleanupEscape()
   {
       if (_centcommMap == null) return;

       _mapManager.DeleteMap(_centcommMap.Value);
   }
}
