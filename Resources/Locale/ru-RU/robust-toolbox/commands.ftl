### Localization for engine console commands


## generic command errors

cmd-invalid-arg-number-error = Недопустимое число аргументов.
cmd-parse-failure-integer = { $arg } не является допустимым integer.
cmd-parse-failure-float = { $arg } не является допустимым float.
cmd-parse-failure-bool = { $arg } не является допустимым bool.
cmd-parse-failure-uid = { $arg } не является допустимым UID сущности.
cmd-parse-failure-entity-exist = UID { $arg } не соответствует существующей сущности.

## 'help' command

cmd-help-desc = Выводит общую справку или справку по определенной команде
cmd-help-help =
    Использование: help [имя команды]
    Если имя команды не будет указано, будет выведена общая справка. Если имя команды будет указано, будет выведена справка по этой команде.
cmd-help-no-args = Чтобы получить справку по определенной команде, используйте 'help <command>'. Для получения списка всех доступных команд используйте 'list'. Для поиска по командам используйте 'list <filter>'.
cmd-help-unknown = Неизвестная команда: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Недопустимое количество аргументов.
cmd-help-arg-cmdname = [имя команды]

## 'cvar' command

cmd-cvar-desc = Получает или устанавливает CVar.
cmd-cvar-help =
    Использование: cvar <name | ?> [значение]
    Если значение предоставлено, оно спарсится и сохранится как новое значение CVar.
    Если нет, отобразится текущее значение CVar.
    Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-invalid-args = Должно быть представлено ровно один или два аргумента.
cmd-cvar-not-registered = CVar '{ $cvar }' не зарегистрирован. Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-parse-error = Входное значение имеет неправильный формат для типа { $type }
cmd-cvar-compl-list = Список доступных CVar-ов
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'list' command

cmd-list-desc = Выводит список доступных команд с опциональным поисковым фильтром
cmd-list-help =
    Использование: list [фильтр]
    Выводит список всех доступных команд. Если был предоставлен аргумент, он будет использоваться для фильтрации команд по имени.
cmd-list-heading = SIDE NAME            DESC{ "\u000A" }-------------------------{ "\u000A" }
cmd-list-arg-filter = [фильтр]

## '>' command, aka remote exec

cmd-remoteexec-desc = Выполняет команду на стороне сервера
cmd-remoteexec-help =
    Использование: > <command> [arg] [arg] [arg...]
    Выполняет команду на стороне сервера. Это необходимо, если на клиенте имеется команда с таким же именем, так как при простом выполнении команды сначала будет запущена команда на клиенте.

## 'gc' command

cmd-gc-desc = Запускает GC (Garbage Collector, Сборка мусора)
cmd-gc-help =
    Использование: gc [поколение]
    Использует GC.Collect() для запуска Сборки мусора.
    Если был предоставлен аргумент, то он спарсится как номер поколения GC и используется GC.Collect(int).
    Используйте команду 'gfc' для проведения сборки мусора, со сжатием 'кучи больших объектов' (LOH-compacting).
cmd-gc-failed-parse = Не удалось спарсить аргумент.
cmd-gc-arg-generation = [поколение]

## 'gcf' command

cmd-gcf-desc = Запускает GC, полную, со сжатием 'кучи больших объектов' (LOH-compacting) и всего.
cmd-gcf-help =
    Использование: gcf
    Выполняет полный GC.Collect(2, GCCollectionMode.Forced, true, true) одновременно сжимая 'кучу больших объектов' LOH.
    Скорее всего, это приведет к зависанию на сотни миллисекунд, имейте в виду.

## 'gc_mode' command

cmd-gc_mode-desc = Изменяет/отображает режим задержки GC
cmd-gc_mode-help =
    Использование: gc_mode [тип]
    Если аргумент не был предоставлен, вернётся текущий режим задержки GC.
    Если аргумент был пропущен, он спарсится как GCLatencyMode и будет установлен как режим задержки GC.
cmd-gc_mode-current = текущий режим задержки gc: { $prevMode }
cmd-gc_mode-possible = возможные режимы:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = неизвестный режим задержки gc: { $arg }
cmd-gc_mode-attempt = попытка изменения режима задержки gc: { $prevMode } -> { $mode }
cmd-gc_mode-result = полученный режим задержки gc: { $mode }
cmd-gc_mode-arg-type = [тип]

## 'mem' command

cmd-mem-desc = Выводит информацию об управляемой памяти
cmd-mem-help = Использование: mem
cmd-mem-report =
    Размер кучи: { TOSTRING($heapSize, "N0") }
    Всего распределено: { TOSTRING($totalAllocated, "N0") }

## 'physics' command

cmd-physics-overlay = { $overlay } не является распознанным оверлеем

## 'lsasm' command

cmd-lsasm-desc = Выводит список загруженных сборок по контексту загрузки
cmd-lsasm-help = Использование: lsasm

## 'exec' command

cmd-exec-desc = Исполняет скриптовый файл из записываемых пользовательских данных игры
cmd-exec-help =
    Использование: exec <fileName>
    Каждая строка в файле выполняется как одна команда, если только она не начинается со знака #
cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command

cmd-dump_net_comps-desc = Выводит таблицу сетевых компонентов.
cmd-dump_net_comps-help = Использование: dump_net-comps
cmd-dump_net_comps-error-writeable = Регистрация все еще доступна для записи, сетевые идентификаторы не были сгенерированы.
cmd-dump_net_comps-header = Регистрации сетевых компонентов:

## 'dump_event_tables' command

cmd-dump_event_tables-desc = Выводит таблицы направленных событий для сущности.
cmd-dump_event_tables-help = Использование: dump_event_tables <entityUid>
cmd-dump_event_tables-missing-arg-entity = Отсутствует аргумент сущности
cmd-dump_event_tables-error-entity = Недопустимая сущность
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command

cmd-monitor-desc = Переключение отладочного монитора в меню F3.
cmd-monitor-help =
    Использование: monitor <name>
    Возможные мониторы: { $monitors }
    Вы также можете использовать специальные значения "-all" и "+all", чтобы соответственно скрыть или показать все мониторы.
cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Недопустимое имя монитора
cmd-monitor-arg-count = Отсутствует аргумент монитора
cmd-monitor-minus-all-hint = Скрывает все мониторы
cmd-monitor-plus-all-hint = Показывает все мониторы

## Mapping commands

cmd-savemap-desc = Сериализует карту на диск. Не будет сохранять карту после инициализации, если это не будет сделано принудительно.
cmd-savemap-help = savemap <MapID> <Path> [force]
cmd-savemap-not-exist = Целевая карта не существует.
cmd-savemap-init-warning = Попытка сохранить карту после инициализации без принудительного сохранения.
cmd-savemap-attempt = Попытка сохранить карту { $mapId } в { $path }.
cmd-savemap-success = Карта успешно сохранена.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]
cmd-loadmap-desc = Загружает карту с диска в игру.
cmd-loadmap-help = loadmap <MapID> <Path> [x] [y] [rotation] [consistentUids]
cmd-loadmap-nullspace = Невозможно загрузить в карту 0.
cmd-loadmap-exists = Карта { $mapId } уже существует.
cmd-loadmap-success = Карта { $mapId } была загружена из { $path }.
cmd-loadmap-error = При загрузке карты из { $path } произошла ошибка.
cmd-hint-loadmap-x-position = [x-position]
cmd-hint-loadmap-y-position = [y-position]
cmd-hint-loadmap-rotation = [rotation]
cmd-hint-loadmap-uids = [float]
cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command


# Примечание: команда flushcookies взята из Robust.Client.WebView, её нет в коде основного движка.

cmd-flushcookies-desc = Сброс хранилища CEF-cookie на диск
cmd-flushcookies-help =
    Это гарантирует правильное сохранение файлов cookie на диске в случае неаккуратного выключения.
    Имейте в виду, что фактическая операция является асинхронной.
