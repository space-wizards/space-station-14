# Playback Commands

cmd-replay-play-desc = Возобновить воспроизведение повтора.
cmd-replay-play-help = replay_play

cmd-replay-pause-desc = Поставить повтор на паузу.
cmd-replay-pause-help = replay_pause

cmd-replay-toggle-desc = Переключить паузу повтора.
cmd-replay-toggle-help = replay_toggle

cmd-replay-toggle-screenshot-mode-desc = Toggles screenshot mode for replays, hiding the replay control widget.
cmd-replay-toggle-screenshot-mode-help = replay_toggle_screenshot_mode

cmd-replay-stop-desc = Остановить и выгрузить повтор.
cmd-replay-stop-help = replay_stop

cmd-replay-load-desc = Загрузить и начать повтор.
cmd-replay-load-help = replay_load <папка повтора>
cmd-replay-load-hint = Replay folder

cmd-replay-skip-desc = Перемотать вперёд или назад.
cmd-replay-skip-help = replay_skip <кол-во тиков или timespan>
cmd-replay-skip-hint = Кол-во тиков или timespan (ЧЧ:ММ:СС).

cmd-replay-set-time-desc = Перемотать вперёд или назад к определённому моменту времени.
cmd-replay-set-time-help = replay_set <тик или время>
cmd-replay-set-time-hint = Тик или timespan (ЧЧ:ММ:СС), начиная с

cmd-replay-error-time = "{ $time }" не является integer или timespan.
cmd-replay-error-args = Недопустимое количество аргументов.
cmd-replay-error-no-replay = Сейчас не проигрывается повтор.
cmd-replay-error-already-loaded = Повтор уже загружен.
cmd-replay-error-run-level = Вы не можете загрузить повтор, пока присоединены к серверу.

# Recording commands

cmd-replay-recording-start-desc = Начинает запись повтора, опционально с временным лимитом.
cmd-replay-recording-start-help = Использование: replay_recording_start [имя] [перезаписать] [временной лимит]
cmd-replay-recording-start-success = Запись повтора началась.
cmd-replay-recording-start-already-recording = Уже ведётся запись.
cmd-replay-recording-start-error = Возникла ошибка во время попытки начать запись.
cmd-replay-recording-start-hint-time = [временной лимит (минуты)]
cmd-replay-recording-start-hint-name = [имя]
cmd-replay-recording-start-hint-overwrite = [перезаписать (bool)]

cmd-replay-recording-stop-desc = Останавливает запись повтора.
cmd-replay-recording-stop-help = Использование: replay_recording_stop
cmd-replay-recording-stop-success = Запись повтора остановлена.
cmd-replay-recording-stop-not-recording = Сейчас не ведётся запись.

cmd-replay-recording-stats-desc = Отображает информацию о текущей записи повтора.
cmd-replay-recording-stats-help = Использование: replay_recording_stats
cmd-replay-recording-stats-result = Продолжительность: { $time } мин, Кол-во тиков: { $ticks }, Размер: { $size } МБ, соотношение: { $rate } МБ/мин.


# Time Control UI
replay-time-box-scrubbing-label = Dynamic Scrubbing
replay-time-box-replay-time-label = Время записи: { $current } / { $end }  ({ $percentage }%)
replay-time-box-server-time-label = Серверное время: { $current } / { $end }
replay-time-box-index-label = Индекс: { $current } / { $total }
replay-time-box-tick-label = Тик: { $current } / { $total }
