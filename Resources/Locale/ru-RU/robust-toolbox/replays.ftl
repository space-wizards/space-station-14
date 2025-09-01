# Playback Commands

cmd-replay-play-desc = Продолжить проигрывание повтора.
cmd-replay-play-help = replay_play
cmd-replay-pause-desc = Приостановить проигрывание повтора.
cmd-replay-pause-help = replay_pause
cmd-replay-toggle-desc = Продолжить или приостановить проигрывание повтора.
cmd-replay-toggle-help = replay_toggle
cmd-replay-stop-desc = Остановить и отгрузить повтор.
cmd-replay-stop-help = replay_stop
cmd-replay-load-desc = Загрузить и начать повтор.
cmd-replay-load-help = replay_load <replay folder>
cmd-replay-load-hint = Папка повторов
cmd-replay-skip-desc = Перемотать вперёд или назад во времени.
cmd-replay-skip-help = replay_skip <tick or timespan>
cmd-replay-skip-hint = Тики или продолжительность (HH:MM:SS).
cmd-replay-set-time-desc = Перескочить вперёд или назад к конкретному времени.
cmd-replay-set-time-help = replay_set <tick or time>
cmd-replay-set-time-hint = Тик или время (HH:MM:SS), начало
cmd-replay-error-time = "{ $time }" не целое число или время.
cmd-replay-error-args = Неправильно количество аргументов.
cmd-replay-error-no-replay = Сейчас не проигрывается повтор.
cmd-replay-error-already-loaded = Повтор уже загружен.
cmd-replay-error-run-level = Вы не можете загрузить повтор, пока вы подключены к серверу.

# Recording commands

cmd-replay-recording-start-desc = Начинает запись повтора, опционально с ограничением времени.
cmd-replay-recording-start-help = Использование: replay_recording_start [имя] [перезаписать] [время]
cmd-replay-recording-start-success = Запись повтора начата.
cmd-replay-recording-start-already-recording = Запись повтора уже идёт.
cmd-replay-recording-start-error = При попытке начать запись повтора возникла ошибка.
cmd-replay-recording-start-hint-time = [время (в минутах)]
cmd-replay-recording-start-hint-name = [имя]
cmd-replay-recording-start-hint-overwrite = [перезаписать (bool)]
cmd-replay-recording-stop-desc = Останавливает запись повтора.
cmd-replay-recording-stop-help = Использование: replay_recording_stop
cmd-replay-recording-stop-success = Останавливает запись повтора.
cmd-replay-recording-stop-not-recording = Повтор сейчас не записывается.
cmd-replay-recording-stats-desc = Отображает информацию о текущей записи повтора.
cmd-replay-recording-stats-help = Использование: replay_recording_stats
cmd-replay-recording-stats-result = Продолжительность: { $time } мин, Тиков: { $ticks }, Размер: { $size } МБ, скорость: { $rate } МБ/мин.
# Time Control UI
replay-time-box-scrubbing-label = Динамический Скруббинг
replay-time-box-replay-time-label = Время Записи: { $current } / { $end }  ({ $percentage }%)
replay-time-box-server-time-label = Время Сервера: { $current } / { $end }
replay-time-box-index-label = Индекс: { $current } / { $total }
replay-time-box-tick-label = Тик: { $current } / { $total }
