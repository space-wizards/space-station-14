### Localization for engine console commands

cmd-hint-float = [float]

## generic command errors

cmd-invalid-arg-number-error = Недопустимое число аргументов.

cmd-parse-failure-integer = { $arg } не является допустимым integer.
cmd-parse-failure-float = { $arg } не является допустимым float.
cmd-parse-failure-bool = { $arg } не является допустимым bool.
cmd-parse-failure-uid = { $arg } не является допустимым UID сущности.
cmd-parse-failure-mapid = { $arg } не является допустимым MapId.
cmd-parse-failure-enum = { $arg } не является { $enum } Enum.
cmd-parse-failure-grid = { $arg } не является допустимым гридом.
cmd-parse-failure-cultureinfo = "{ $arg }" не является допустимым CultureInfo.
cmd-parse-failure-entity-exist = UID { $arg } не соответствует существующей сущности.
cmd-parse-failure-session = Не существует сессии с именем пользователя: { $username }

cmd-error-file-not-found = Не удалось найти файл: { $file }.
cmd-error-dir-not-found = Не удалось найти директорию: { $dir }.

cmd-failure-no-attached-entity = Нет сущности привязанной к этой оболочке.

## 'help' command
cmd-help-desc = Выводит общую справку или справку по определённой команде.
cmd-help-help = Использование: { $command } [имя команды]
    Если имя команды не будет указано, будет выведена общая справка. Если имя команды будет указано, будет выведена справка по этой команде.

cmd-help-no-args = Чтобы получить справку по определённой команде, используйте 'help <command>'. Для получения списка всех доступных команд используйте 'list'. Для поиска по командам используйте 'list <filter>'.
cmd-help-unknown = Неизвестная команда: { $command }
cmd-help-top = { $command } — { $description }
cmd-help-invalid-args = Недопустимое количество аргументов.
cmd-help-arg-cmdname = [имя команды]

## 'cvar' command
cmd-cvar-desc = Получает или устанавливает CVar.
cmd-cvar-help = Использование: { $command } <имя | ?> [значение]
    Если значение предоставлено, оно спарсится и сохранится как новое значение CVar.
    Если нет, отобразится текущее значение CVar.
    Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.

cmd-cvar-invalid-args = Должно быть представлено ровно один или два аргумента.
cmd-cvar-not-registered = CVar '{ $cvar }' не зарегистрирован. Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-parse-error = Введённое значение имеет неправильный формат для типа { $type }
cmd-cvar-compl-list = Список доступных CVar-ов
cmd-cvar-arg-name = <имя | ?>
cmd-cvar-value-hidden = <value hidden>

## 'cvar_subs' command
cmd-cvar_subs-desc = Выводит список OnValueChanged на которые подписал CVar.
cmd-cvar_subs-help = Использование: { $command } <имя>

cmd-cvar_subs-invalid-args = Должно быть представлено ровно один аргумент.
cmd-cvar_subs-arg-name = <имя>

## 'list' command
cmd-list-desc = Выводит список доступных команд с опциональным поисковым фильтром.
cmd-list-help = Использование: { $command } [фильтр]
    Выводит список всех доступных команд. Если был предоставлен аргумент, он будет использоваться для фильтрации команд по имени.

cmd-list-heading = SIDE NAME            DESC{ "\u000A" }-------------------------{ "\u000A" }

cmd-list-arg-filter = [фильтр]

## '>' command, aka remote exec
cmd-remoteexec-desc = Выполняет команду на стороне сервера.
cmd-remoteexec-help = Использование: > <command> [arg] [arg] [arg...]
    Выполняет команду на стороне сервера. Это необходимо, если на клиенте имеется команда с таким же именем, так как при простом выполнении команды сначала будет запущена команда на клиенте.

## 'gc' command
cmd-gc-desc = Запускает GC (Garbage Collector, Сборка мусора)
cmd-gc-help = Использование: { $command } [поколение]
    Использует GC.Collect() для запуска Сборки мусора.
    Если был предоставлен аргумент, то он спарсится как номер поколения GC и используется GC.Collect(int).
    Используйте команду 'gfc' для проведения сборки мусора со сжатием 'кучи больших объектов' (LOH-compacting).
cmd-gc-failed-parse = Не удалось спарсить аргумент.
cmd-gc-arg-generation = [поколение]

## 'gcf' command
cmd-gcf-desc = Запускает GC, полную, со сжатием 'кучи больших объектов' (LOH-compacting) и всего.
cmd-gcf-help = Использование: { $command }
    Выполняет полный GC.Collect(2, GCCollectionMode.Forced, true, true) одновременно сжимая 'кучу больших объектов' LOH.
    Скорее всего, это приведёт к зависанию на сотни миллисекунд, имейте в виду.

## 'gc_mode' command
cmd-gc_mode-desc = Изменяет/отображает режим задержки GC
cmd-gc_mode-help = Использование: { $command } [type]
    Если аргумент не был предоставлен, вернётся текущий режим задержки GC.
    Если аргумент был предоставлен, он спарсится как GCLatencyMode и будет установлен как режим задержки GC.

cmd-gc_mode-current = текущий режим задержки gc: { $prevMode }
cmd-gc_mode-possible = возможные режимы:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = неизвестный режим задержки gc: { $arg }
cmd-gc_mode-attempt = попытка изменения режима задержки gc: { $prevMode } -> { $mode }
cmd-gc_mode-result = полученный режим задержки gc: { $mode }
cmd-gc_mode-arg-type = [тип]

## 'mem' command
cmd-mem-desc = Выводит информацию об управляемой памяти.
cmd-mem-help = Использование: { $command }

cmd-mem-report = Размер кучи: { TOSTRING($heapSize, "N0") }
    Всего распределено: { TOSTRING($totalAllocated, "N0") }

## 'physics' command
cmd-physics-overlay = { $overlay } не является распознанным оверлеем

## 'lsasm' command
cmd-lsasm-desc = Выводит список загруженных сборок по контексту загрузки.
cmd-lsasm-help = Использование: lsasm

## 'exec' command
cmd-exec-desc = cmd-exec-desc = Исполняет скриптовый файл из записываемых пользовательских данных игры.
cmd-exec-help = Использование: { $command } <имя файла>
    Каждая строка в файле выполняется как одна команда, если только она не начинается со знака #

cmd-exec-arg-filename = <имя файла>

## 'dump_net_comps' command
cmd-dump_net_comps-desc = Выводит таблицу сетевых компонентов.
cmd-dump_net_comps-help = Использование: { $command }

cmd-dump_net_comps-error-writeable = Регистрация всё ещё доступна для записи, сетевые идентификаторы не были сгенерированы.
cmd-dump_net_comps-header = Регистрации сетевых компонентов:

## 'dump_event_tables' command
cmd-dump_event_tables-desc = cmd-dump_event_tables-desc = Выводит таблицы направленных событий для сущности.
cmd-dump_event_tables-help = Использование: { $command } <entityUid>

cmd-dump_event_tables-missing-arg-entity = Отсутствует аргумент сущности
cmd-dump_event_tables-error-entity = Недопустимая сущность
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command
cmd-monitor-desc = cmd-monitor-desc = Переключение отладочного монитора в меню F3.
cmd-monitor-help = Использование: { $command } <имя>
    Возможные мониторы: { $monitors }
    Вы также можете использовать специальные значения "-all" и "+all", чтобы соответственно скрыть или показать все мониторы.

cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Недопустимое имя монитора
cmd-monitor-arg-count = Отсутствует аргумент монитора
cmd-monitor-minus-all-hint = Скрывает все мониторы
cmd-monitor-plus-all-hint = Показывает все мониторы


## 'setambientlight' command
cmd-set-ambient-light-desc = Позволяет установить окружающее освещение для указанной карты, в формате SRGB.
cmd-set-ambient-light-help = Использование: { $command } [mapid] [r g b a]
cmd-set-ambient-light-parse = Не удалось спарсить аргументы как байтовые значения цветов.

## Mapping commands

cmd-savemap-desc = Сохраняет карту на диск. Не будет сохранять карту после инициализации, если это не будет сделано принудительно.
cmd-savemap-help = Использование: { $command } <MapID> <Путь> [принудительно]
cmd-savemap-not-exist = Целевая карта не существует.
cmd-savemap-init-warning = Попытка сохранить карту после инициализации без принудительного сохранения.
cmd-savemap-attempt = Попытка сохранить карту { $mapId } в { $path }.
cmd-savemap-success = Карта успешно сохранена.
cmd-savemap-error = Не удалось сохранить карту! См. серверные логи для деталей.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Путь>
cmd-hint-savemap-force = [bool]

cmd-loadmap-desc = Loads a map from disk into the game.
cmd-loadmap-help = Использование: { $command } <MapID> <Путь> [x] [y] [вращение] [consistentUids]
cmd-loadmap-nullspace = Вы не можеге загрузить в карту 0.
cmd-loadmap-exists = Карта { $mapId } уже существует.
cmd-loadmap-success = Карта { $mapId } была загружена из { $path }.
cmd-loadmap-error = При загрузке карты из { $path } произошла ошибка.
cmd-hint-loadmap-x-position = [позиция x]
cmd-hint-loadmap-y-position = [позиция y]
cmd-hint-loadmap-rotation = [вращение]
cmd-hint-loadmap-uids = [float]

cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

cmd-flushcookies-desc = Сброс хранилища CEF-cookie на диск.
cmd-flushcookies-help = Использование: { $command }
    Это гарантирует правильное сохранение файлов cookie на диске в случае неправильного выключения.
    Имейте в виду, что фактическая операция является асинхронной.

cmd-ldrsc-desc = Предварительно кэширует ресурс.
cmd-ldrsc-help = Использование: { $command } <path> <type>

cmd-rldrsc-desc = Перезагружает ресурсы.
cmd-rldrsc-help = Использование: { $command } <path> <type>

cmd-gridtc-desc = Получить количество плиток в гриде.
cmd-gridtc-help = Использование: { $command } <gridId>


# Client-side commands
cmd-guidump-desc = Дамп дерева интерфейса в /guidump.txt в данные пользователя.
cmd-guidump-help = Использование: { $command }

cmd-uitest-desc = Открыть UI окно для тестирования.
cmd-uitest-help = Использование: { $command }

## 'uitest2' command
cmd-uitest2-desc = Открывает UI контрольного тестирования ОС.
cmd-uitest2-help = Использование: { $command } <tab>
cmd-uitest2-arg-tab = <tab>
cmd-uitest2-error-args = Ожидается не более одного аргумента
cmd-uitest2-error-tab = Недопустимая вкладка: '{ $value }'
cmd-uitest2-title = UITest2


cmd-setclipboard-desc = Устанавливает системный буфер обмена.
cmd-setclipboard-help = Использование: { $command } <text>

cmd-getclipboard-desc = Получает системный буфер обмена.
cmd-getclipboard-help = Использование: { $command }

cmd-togglelight-desc = Переключает рендеринг света.
cmd-togglelight-help = Использование: { $command }

cmd-togglefov-desc = Переключает поле зрения клиента.
cmd-togglefov-help = Использование: { $command }

cmd-togglehardfov-desc = Включает жёсткое поле зрения клиента. (для отладки space-station-14#2353)
cmd-togglehardfov-help = Использование: { $command }

cmd-toggleshadows-desc = Переключение рендеринга теней.
cmd-toggleshadows-help = Использование: { $command }

cmd-togglelightbuf-desc = Переключение рендеринга освещения. Сюда входят тени, но не поле зрения.
cmd-togglelightbuf-help = Использование: { $command }

cmd-chunkinfo-desc = Получает информацию о чанке под курсором мыши.
cmd-chunkinfo-help = Использование: { $command }

cmd-rldshader-desc = Перезагружает все шейдеры.
cmd-rldshader-help = Использование: { $command }

cmd-cldbglyr-desc = Переключение слоёв отладки поле зрения и освещения.
cmd-cldbglyr-help= Использование: { $command } <layer>: Toggle <layer>
    cldbglyr: Отключить все слои

cmd-key-info-desc = Информация о ключе для клавиши.
cmd-key-info-help = Использование: { $command } <Key>

## 'bind' command
cmd-bind-desc = Привязывает комбинацию клавиш ввода с командой ввода.
cmd-bind-help = Использование: { $command } { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Обратите внимание, что это НЕ сохраняет привязки автоматически.
    Используйте команду 'svbind', чтобы сохранить конфигурацию привязки.

cmd-bind-arg-key = <KeyName>
cmd-bind-arg-mode = <BindMode>
cmd-bind-arg-command = <InputCommand>

cmd-net-draw-interp-desc = Переключает отладочный рисунок сетевой интерполяции.
cmd-net-draw-interp-help = Использование: { $command }

cmd-net-watch-ent-desc = Выводит на консоль все сетевые обновления для EntityId..
cmd-net-watch-ent-help = Использование: { $command } <0|EntityUid>

cmd-net-refresh-desc = Запрашивает полное состояние сервера.
cmd-net-refresh-help = Использование: { $command }

cmd-net-entity-report-desc = Переключает панель отчёта о сетевых сущностях.
cmd-net-entity-report-help = Использование: { $command }

cmd-fill-desc = Заполнить консоль для отладки.
cmd-fill-help = Использование: { $command }
                Заполняет консоль всякой чепухой для отладки.

cmd-cls-desc = Очищает консоль.
cmd-cls-help = Использование: { $command }
               Очищает консоль отладки от всех сообщений.

cmd-sendgarbage-desc = Отправляет мусор на сервер.
cmd-sendgarbage-help = Использование: { $command }
                       Сервер ответит "нет ты".

cmd-loadgrid-desc = Загружает грид из файла на существующую карту.
cmd-loadgrid-help = Использование: { $command } <MapID> <Путь> [x y] [вращение] [storeUids]

cmd-loc-desc = Выводит абсолютное местоположение сущности игрока в консоль.
cmd-loc-help = Использование: { $command }

cmd-tpgrid-desc = Телепортирует грид в новое место.
cmd-tpgrid-help = Использование: { $command } <gridId> <X> <Y> [<MapId>]

cmd-rmgrid-desc = Удаляет грид с карты. Вы не можете удалить стандартный грид.
cmd-rmgrid-help = Использование: { $command } <gridId>

cmd-mapinit-desc = Запускает инициализацию карты на карте.
cmd-mapinit-help = Использование: { $command } <mapID>

cmd-lsmap-desc = Перечисляет карты.
cmd-lsmap-help = Использование: { $command }

cmd-lsgrid-desc = Перечисляет гриды.
cmd-lsgrid-help = Использование: { $command }

cmd-addmap-desc = Добавляет в раунд новую пустую карту. Если mapID уже существует, то команда ничего не сделает.
cmd-addmap-help = Использование: { $command } <mapID> [pre-init]

cmd-rmmap-desc = Удаляет карту из мира. Вы не можете удалить nullspace.
cmd-rmmap-help = Использование: { $command } <mapId>

cmd-savegrid-desc = Сохраняет грид на диск.
cmd-savegrid-help = Использование: { $command } <gridID> <Path>

cmd-testbed-desc = Загружает физический испытательный стенд на указанной карте.
cmd-testbed-help = Использование: { $command } <mapid> <test>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

## 'addcomp' command
cmd-addcomp-desc = Добавляет компонент к сущности.
cmd-addcomp-help = Использование: { $command } <uid> <имя компонента>
cmd-addcompc-desc = Добавляет компонент к сущности на стороне клиента.
cmd-addcompc-help = Использование: { $command } <uid> <имя компонента>

## 'rmcomp' command
cmd-rmcomp-desc = Удаляет компонент у сущности.
cmd-rmcomp-help = Использование: { $command } <uid> <имя компонента>
cmd-rmcompc-desc = Удаляет компонент у сущности на стороне клиента.
cmd-rmcompc-help = Использование: { $command } <uid> <имя компонента>

## 'addview' command
cmd-addview-desc = Позволяет подписаться на просмотр сущности в целях отладки.
cmd-addview-help = Использование: { $command } <entityUid>
cmd-addviewc-desc = Позволяет подписаться на просмотр сущности в целях отладки.
cmd-addviewc-help = Использование: { $command } <entityUid>

## 'removeview' command
cmd-removeview-desc = Позволяет отписаться от просмотра сущности в целях отладки.
cmd-removeview-help = Использование: { $command } <entityUid>

## 'loglevel' command
cmd-loglevel-desc = Изменяет уровень логирования для предоставленного sawmill.
cmd-loglevel-help = Использование: { $command } <sawmill> <уровень>
      sawmill: Метка, которая префиксирует сообщения логов. Именно для неё вы устанавливаете уровень.
      level: Уровень логирования. Должно соответствовать одному из значений перечисления LogLevel.

cmd-testlog-desc = Записывает протокол тестов в sawmill.
cmd-testlog-help = Использование: { $command } <sawmill> <уровень> <сообщение>
    sawmill: Метка, префиксируемая логированному сообщению.
    level: Уровень логирования. Должно соответствовать одному из значений перечисления LogLevel.
    message: Логируемое сообщение. Заключите в двойные кавычки, если хотите использовать пробелы.

## 'vv' command
cmd-vv-desc = Открывает просмотр переменных.
cmd-vv-help = Использование: { $command } <entity UID|IoC имя интерфейса|SIoC имя интерфейса>

## 'showvelocities' command
cmd-showvelocities-desc = Отображает угловую и линейную скорости.
cmd-showvelocities-help = Использование: { $command }

## 'setinputcontext' command
cmd-setinputcontext-desc = Устанавливает активный контекст ввода.
cmd-setinputcontext-help = Использование: { $command } <context>

## 'forall' command
cmd-forall-desc = Запускает команду для всех сущностей с данным компонентом.
cmd-forall-help = Использование: { $command } <bql query> do <command...>

## 'delete' command
cmd-delete-desc = Удаляет сущность с указанным ID.
cmd-delete-help = Использование: { $command } <entity UID>

# System commands
cmd-showtime-desc = Показывает время сервера.
cmd-showtime-help = Использование: { $command }

cmd-restart-desc = Корректно перезапускает сервер (не только раунд).
cmd-restart-help = Использование: { $command }

cmd-shutdown-desc = Корректно выключает сервер.
cmd-shutdown-help = Использование: { $command }

cmd-saveconfig-desc = Сохраняет конфигурацию клиента в файл конфигурации.
cmd-saveconfig-help = Использование: { $command }

cmd-netaudit-desc = Выводит информацию о безопасности NetMsg..
cmd-netaudit-help = Использование: { $command }

# Player commands
cmd-tp-desc = Телепортирует игрока в любую точку в раунде.
cmd-tp-help = Использование: { $command } <x> <y> [<mapID>]

cmd-tpto-desc = Телепортирует текущего игрока или указанных игроков/сущностей к местоположению первого игрока/сущности.
cmd-tpto-help = Использование: { $command } <имя пользователя|uid> [имя пользователя|NetEntity]...
cmd-tpto-destination-hint = точка назначения (NetEntity или имя пользователя)
cmd-tpto-victim-hint = телепортируемая сущность (NetEntity или имя пользователя)
cmd-tpto-parse-error = Не удается разрешить сущность или игрока: { $str }

cmd-listplayers-desc = Перечисляет всех игроков, подключённых в данный момент.
cmd-listplayers-help = Использование: { $command }

cmd-kick-desc = Кикает подключённого игрока с сервера, отключая его.
cmd-kick-help = Использование: { $command } <PlayerIndex> [<Причина>]

# Spin command
cmd-spin-desc = Заставляет сущность вращаться. Сущность по умолчанию является надклассом прикреплённого игрока.
cmd-spin-help = Использование: { $command } velocity [drag] [entityUid]

# Localization command
cmd-rldloc-desc = Перезагружает локализацию (клиент и сервер).
cmd-rldloc-help = Использование: { $command }

# Debug entity controls
cmd-spawn-desc = Создаёт сущность определённого типа.
cmd-spawn-help = Использование: { $command } <прототип> | { $command } <прототип> <относительное ID сущности> | { $command } <прототип> <x> <y>
cmd-cspawn-desc = Спавнит сущность определённого типа у ваших ног на стороне клиента.
cmd-cspawn-help = Использование: { $command } <тип сущности>

cmd-dumpentities-desc = Дамп списка объектов.
cmd-dumpentities-help = Использование: { $command }
                        Выводит список объектов с UID и прототипом.

cmd-getcomponentregistration-desc = Получает информацию о регистрации компонента.
cmd-getcomponentregistration-help = Использование: { $command } <имя компонента>

cmd-showrays-desc = Переключает отладку отображения физических лучей. Необходимо указать целое число для <продлжительности жизни луча>.
cmd-showrays-help = Использование: { $command } <продлжительность жизни луча>

cmd-disconnect-desc = Немедленно отключиться от сервера и вернуться в главное меню.
cmd-disconnect-help = Использование: { $command }

cmd-entfo-desc = Отображает подробную диагностику сущности.
cmd-entfo-help = Использование: { $command } <entityuid>
    UID сущности может иметь префикс 'c', чтобы быть преобразованной в UID клиентской сущности.

cmd-fuck-desc = Вызывает исключение.
cmd-fuck-help = Использование: { $command }

cmd-showpos-desc = Включает отрисовку для всех позиций сущностей на экране.
cmd-showpos-help = Использование: { $command }

cmd-showrot-desc = Включает отрисовку для всех вращений сущностей на экране.
cmd-showrot-help = Использование: { $command }

cmd-showvel-desc = Включает отрисовку для всех ускорений сущностей на экране.
cmd-showvel-help = Использование: { $command }

cmd-showangvel-desc = Включает отрисовку для всех угловых ускорений сущностей на экране.
cmd-showangvel-help = Использование: { $command }

cmd-sggcell-desc = Перечисляет сущности в ячейке сетки привязки..
cmd-sggcell-help = Использование: { $command } <gridID> <vector2i>
    Этот vector2i параметр в форме x<int>,y<int>.

cmd-overrideplayername-desc = Изменяет имя, используемое при попытке подключения к серверу.
cmd-overrideplayername-help = Использование: { $command } <имя>

cmd-showanchored-desc = Показывает закреплённые объекты на определённой плитке.
cmd-showanchored-help = Использование: { $command }

cmd-dmetamem-desc = Выводит члены типа в формате, подходящем для файла конфигурации песочницы.
cmd-dmetamem-help = Использование: { $command } <тип>

cmd-launchauth-desc = Загрузить токены аутентификации из данных лаунчера, чтобы облегчить тестирование работающих серверов.
cmd-launchauth-help = Использование: { $command } <имя аккаунта>

cmd-lightbb-desc = Переключить отображение световой ограничительной рамки.
cmd-lightbb-help = Использование: { $command }

cmd-monitorinfo-desc = Информация о мониторах.
cmd-monitorinfo-help = Использование: { $command } <id>

cmd-setmonitor-desc = Установить монитор.
cmd-setmonitor-help = Использование: { $command } <id>

cmd-physics-desc = Показывает наложение отладочной физики. Аргумент определяет наложение.
cmd-physics-help = Использование: { $command } <aabbs / com / contactnormals / contactpoints / distance / joints / shapeinfo / shapes>

cmd-hardquit-desc = Мгновенно убивает игровой клиент.
cmd-hardquit-help = Использование: { $command }
                    Мгновенно убивает игровой клиент, не оставляя следов. Не говорит серверу пока.

cmd-quit-desc = Корректное завершение работы клиента игры.
cmd-quit-help = Использование: { $command }
                Правильно завершает работу игрового клиента, уведомляя об этом подключённый сервер и т.д.

cmd-csi-desc = Открывает интерактивную консоль C#.
cmd-csi-help = Использование: { $command }

cmd-scsi-desc = Открывает интерактивную консоль C# на сервере.
cmd-scsi-help = Использование: { $command }

cmd-watch-desc = Открывает окно просмотра переменных.
cmd-watch-help = Использование: { $command }

cmd-showspritebb-desc = Переключить отображение границ спрайта.
cmd-showspritebb-help = Использование: { $command }

cmd-togglelookup-desc = Показывает/скрывает границы списка сущностей с помощью наложения.
cmd-togglelookup-help = Использование: { $command }

cmd-net_entityreport-desc = Переключает панель отчёта о сетевых сущностях.
cmd-net_entityreport-help = Использование: { $command }

cmd-net_refresh-desc = Запрашивает полное состояние сервера.
cmd-net_refresh-help = Использование: { $command }

cmd-net_graph-desc = Переключает панель статистики сети.
cmd-net_graph-help = Использование: { $command }

cmd-net_watchent-desc = Выводит в консоль все сетевые обновления для EntityId.
cmd-net_watchent-help = Использование: { $command } <0|EntityUid>

cmd-net_draw_interp-desc = Переключает отладочную отрисовку сетевой интерполяции.
cmd-net_draw_interp-help = Использование: { $command } <0|EntityUid>

cmd-vram-desc = Отображает статистику использования видеопамяти игрой.
cmd-vram-help = Использование: { $command }

cmd-showislands-desc = Показывает текущие физические тела, задействованные в каждом physics island.
cmd-showislands-help = Использование: { $command }

cmd-showgridnodes-desc = Показывает узлы для разделения грида.
cmd-showgridnodes-help = Использование: { $command }

cmd-profsnap-desc = Сделать снимок профилирования.
cmd-profsnap-help = Использование: { $command }

cmd-devwindow-desc = Окно разработки.
cmd-devwindow-help = Использование: { $command }

cmd-scene-desc = Немедленно сменяет UI сцены/состояния..
cmd-scene-help = Использование: { $command } <className>

cmd-szr_stats-desc = Сообщить статистику сериализатора.
cmd-szr_stats-help = Использование: { $command }

cmd-hwid-desc = Возвращает текущий HWID (HardWare ID).
cmd-hwid-help = Использование: { $command }

cmd-vvread-desc = Получить значение пути с помощью VV (Просмотра переменных/View Variables).
cmd-vvread-help = Использование: { $command } <путь>

cmd-vvwrite-desc = зменить значение пути с помощью VV (Просмотра переменных/View Variables).
cmd-vvwrite-help = Использование: { $command } <путь>

cmd-vvinvoke-desc = Вызов/запуск пути с аргументами с помощью VV.
cmd-vvinvoke-help = Использование: { $command } <путь> [arguments...]

cmd-dump_dependency_injectors-desc = Дамп кэша инжектора зависимостей IoCManager.
cmd-dump_dependency_injectors-help = Использование: { $command }
cmd-dump_dependency_injectors-total-count = Общее количество: { $total }

cmd-dump_netserializer_type_map-desc = Дамп карты типов NetSerializer и хеша сериализатора.
cmd-dump_netserializer_type_map-help = Использование: { $command }

cmd-hub_advertise_now-desc = Немедленно разместить сервер в мастер хабе.
cmd-hub_advertise_now-help = Использование: { $command }

cmd-echo-desc = Вывести аргументы в обратно консоль.
cmd-echo-help = Использование: { $command } "<сообшение>"

## 'vfs_ls' command
cmd-vfs_ls-desc = Перечислить содержимое каталогов в VFS.
cmd-vfs_ls-help = Использование: { $command } <путь>
    Пример:
    vfs_list /Assemblies

cmd-vfs_ls-err-args = Нужен ровно 1 аргумент.
cmd-vfs_ls-hint-path = <путь>

cmd-reloadtiletextures-desc = Перезагрузить атлас текстур плиток для разрешения быстрой перезагрузки спрайтов плиток.
cmd-reloadtiletextures-help = Использование: { $command }

cmd-audio_length-desc = Отобразить длинну аудиофайла
cmd-audio_length-help = Использование: { $command } { cmd-audio_length-arg-file-name }
cmd-audio_length-arg-file-name = <имя файла>

## PVS
cmd-pvs-override-info-desc = Выводит информацию о любых перезаписях PVS связанных с сущностью.
cmd-pvs-override-info-empty = У сущности { $nuid } нет перезаписей PVS.
cmd-pvs-override-info-global = У сущности { $nuid } есть глобальная перезапись.
cmd-pvs-override-info-clients = У сущности { $nuid } есть сессионная перезапись для { $clients }.

cmd-localization_set_culture-desc = Установить DefaultCulture для клиентского LocalizationManager.
cmd-localization_set_culture-help = Использование: { $command } <cultureName>
cmd-localization_set_culture-culture-name = <cultureName>
cmd-localization_set_culture-changed = Локализация изменена на { $code } ({ $nativeName } / { $englishName })

cmd-addmap-hint-2 = runMapInit [true / false]
