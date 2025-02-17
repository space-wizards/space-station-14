# Playback Commands

cmd-replay-play-desc = Resume replay playback.
cmd-replay-play-help = replay_play
cmd-replay-pause-desc = Pause replay playback
cmd-replay-pause-help = replay_pause
cmd-replay-toggle-desc = Resume or pause replay playback.
cmd-replay-toggle-help = replay_toggle
cmd-replay-stop-desc = Stop and unload a replay.
cmd-replay-stop-help = replay_stop
cmd-replay-load-desc = Load and start a replay.
cmd-replay-load-help = replay_load <replay folder>
cmd-replay-load-hint = Replay folder
cmd-replay-skip-desc = Skip forwards or backwards in time.
cmd-replay-skip-help = replay_skip <tick or timespan>
cmd-replay-skip-hint = Ticks or timespan (HH:MM:SS).
cmd-replay-set-time-desc = Jump forwards or backwards to some specific time.
cmd-replay-set-time-help = replay_set <tick or time>
cmd-replay-set-time-hint = Tick or timespan (HH:MM:SS), starting from
cmd-replay-error-time = "{ $time }" is not an integer or timespan.
cmd-replay-error-args = Wrong number of arguments.
cmd-replay-error-no-replay = Not currently playing a replay.
cmd-replay-error-already-loaded = A replay is already loaded.
cmd-replay-error-run-level = You cannot load a replay while connected to a server.

# Recording commands

cmd-replay-recording-start-desc = Starts a replay recording, optionally with some time limit.
cmd-replay-recording-start-help = Usage: replay_recording_start [name] [overwrite] [time limit]
cmd-replay-recording-start-success = Started recording a replay.
cmd-replay-recording-start-already-recording = Already recording a replay.
cmd-replay-recording-start-error = An error occurred while trying to start the recording.
cmd-replay-recording-start-hint-time = [time limit (minutes)]
cmd-replay-recording-start-hint-name = [name]
cmd-replay-recording-start-hint-overwrite = [overwrite (bool)]
cmd-replay-recording-stop-desc = Stops a replay recording.
cmd-replay-recording-stop-help = Usage: replay_recording_stop
cmd-replay-recording-stop-success = Stopped recording a replay.
cmd-replay-recording-stop-not-recording = Not currently recording a replay.
cmd-replay-recording-stats-desc = Displays information about the current replay recording.
cmd-replay-recording-stats-help = Usage: replay_recording_stats
cmd-replay-recording-stats-result = Duration: { $time } min, Ticks: { $ticks }, Size: { $size } mb, rate: { $rate } mb/min.
# Time Control UI
replay-time-box-scrubbing-label = Dynamic Scrubbing
replay-time-box-replay-time-label = Recording Time: { $current } / { $end }  ({ $percentage }%)
replay-time-box-server-time-label = Server Time: { $current } / { $end }
replay-time-box-index-label = Index: { $current } / { $total }
replay-time-box-tick-label = Tick: { $current } / { $total }
