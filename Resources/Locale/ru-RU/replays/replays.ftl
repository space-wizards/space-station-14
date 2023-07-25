# Loading Screen

replay-loading = Загрузка ({$cur}/{$total})
replay-loading-reading = Чтение файлов
replay-loading-processing = Обработка файлов
replay-loading-spawning = Создание сущностей
replay-loading-initializing = Инициализация сущностей
replay-loading-starting= Начальные сущности
replay-loading-failed = Не удалось загрузить повтор:
                        {$reason}

# Main Menu
replay-menu-subtext = Клиент воспроизведения
replay-menu-load = Загрузить выбранный повтор
replay-menu-select = Выбрать повтор
replay-menu-open = Открыть папку с повторами
replay-menu-none = Повторы не найдены.

# Main Menu Info Box
replay-info-title = Информация о воспроизведении
replay-info-none-selected = Повтор не выбран
replay-info-invalid = [color=red]Выбран неверный повтор[/color]
replay-info-info = {"["}color=gray]Выбрано:[/color] {$name} ({$file})
                   {"["}color=gray]Время:[/color] {$time}
                   {"["}color=gray]Идентификатор раунда:[/color] {$roundId}
                   {"["}color=grey]Продолжительность:[/color] {$duration}
                   {"["}color=gray]ForkId:[/color] {$forkId}
                   {"["}color=grey]Версия:[/color] {$version}
                   {"["}color=grey]Движок:[/color] {$engVersion}
                   {"["}color=grey]Хэш типа:[/color] {$hash}
                   {"["}color=gray]Хэш комп.:[/color] {$compHash}

# Replay selection window
replay-menu-select-title = Выберите повтор

# Replay related verbs
replay-verb-spectate = Наблюдать

# command
cmd-replay-spectate-help = replay_spectate [optional entity]
cmd-replay-spectate-desc = Присоединяет или отсоединяет локального игрока к данному идентификатору сущности.
cmd-replay-spectate-hint = Необязательный EntityUid
