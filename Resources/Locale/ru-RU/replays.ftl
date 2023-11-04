# Playback Commands

cmd-replay-play-desc = Возобновить воспроизведение повтора.
cmd-replay-play-help = replay_play

cmd-replay-pause-desc = Приостановить воспроизведение повтора
cmd-replay-pause-help = повтор_паузы

cmd-replay-toggle-desc = Возобновить или приостановить воспроизведение повтора.
cmd-replay-toggle-help = replay_toggle

cmd-replay-stop-desc = Остановить и выгрузить повтор.
cmd-replay-stop-help = replay_stop

cmd-replay-load-desc = Загрузить и запустить повтор.
cmd-replay-load-help = replay_load <папка воспроизведения>
cmd-replay-load-hint = Папка воспроизведения

cmd-replay-skip-desc = Перейти вперед или назад по времени.
cmd-replay-skip-help = replay_skip <тик или временной интервал>
cmd-replay-skip-hint = Такты или временной интервал (ЧЧ:ММ:СС).

cmd-replay-set-time-desc = Перейти вперед или назад к определенному времени.
cmd-replay-set-time-help = replay_set <тик или время>
cmd-replay-set-time-hint = Тик или промежуток времени (ЧЧ:ММ:СС), начиная с

cmd-replay-error-time = "{$time}" не является целым числом или временным интервалом.
cmd-replay-error-args = Неверное количество аргументов.
cmd-replay-error-no-replay = Повтор не воспроизводится.
cmd-replay-error-already-loaded = Повтор уже загружен.
cmd-replay-error-run-level = Вы не можете загрузить повтор при подключении к серверу.

# Recording commands

cmd-replay-recording-start-desc = Запускает запись повтора, опционально с некоторым ограничением по времени.
cmd-replay-recording-start-help = Использование: replay_recording_start [имя] [перезапись] [ограничение по времени]
cmd-replay-recording-start-success = Начата запись повтора.
cmd-replay-recording-start-already-recording = Уже записываю повтор.
cmd-replay-recording-start-error = Произошла ошибка при попытке начать запись.
cmd-replay-recording-start-hint-time = [лимит времени (минуты)]
cmd-replay-recording-start-hint-name = [имя]
cmd-replay-recording-start-hint-overwrite = [перезаписать (bool)]

cmd-replay-recording-stop-desc = Останавливает запись повтора.
cmd-replay-recording-stop-help = Использование: replay_recording_stop
cmd-replay-recording-stop-success = Остановлена запись повтора.
cmd-replay-recording-stop-not-recording = В настоящее время повтор не записывается.

cmd-replay-recording-stats-desc = Отображает информацию о текущей записи повтора.
cmd-replay-recording-stats-help = Использование: replay_recording_stats
cmd-replay-recording-stats-result = Продолжительность: {$time} мин, Такты: {$ticks}, Размер: {$size} МБ, Скорость: {$rate} МБ/мин.


# Time Control UI
replay-time-box-scrubbing-label = Динамическая очистка
replay-time-box-replay-time-label = Время записи: {$current} / {$end} ({$percentage}%)
replay-time-box-server-time-label = Время сервера: {$current} / {$end}
replay-time-box-index-label = Индекс: {$current} / {$total}
replay-time-box-tick-label = Отметка: {$current} / {$total}
