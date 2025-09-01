## General stuff

ui-options-title = Игровые настройки
ui-options-tab-accessibility = Доступность
ui-options-tab-admin = Админ
ui-options-tab-graphics = Графика
ui-options-tab-controls = Управление
ui-options-tab-audio = Аудио
ui-options-tab-network = Сеть
ui-options-tab-misc = Основные
ui-options-apply = Применить
ui-options-reset-all = Сбросить всё и применить
ui-options-default = По-умолчанию
ui-options-value-percent = { TOSTRING($value, "P0") }

# Misc/General menu

ui-options-discordrich = Включить отображение статуса discord
ui-options-general-ui-style = Стиль UI
ui-options-general-discord = Discord
ui-options-general-cursor = Курсор
ui-options-general-speech = Речь
ui-options-general-storage = Инвентарь
ui-options-general-accessibility = Специальные возможности

## Audio menu

ui-options-master-volume = Основная громкость:
ui-options-midi-volume = Громкость MIDI (Муз. инструменты):
ui-options-ambient-music-volume = Громкость музыки окружения:
ui-options-ambience-volume = Громкость окружения:
ui-options-lobby-volume = Громкость лобби и окончания раунда:
ui-options-interface-volume = Громкость интерфейса:
ui-options-ambience-max-sounds = Кол-во одновременных звуков окружения:
ui-options-lobby-music = Музыка в лобби
ui-options-restart-sounds = Звуки перезапуска раунда
ui-options-event-music = Музыка событий
ui-options-admin-sounds = Музыка админов
ui-options-bwoink-sound = Звук уведомления Ахелпа
ui-options-volume-label = Громкость
ui-options-volume-percent = { TOSTRING($volume, "P0") }
ui-options-display-label = Дисплей
ui-options-quality-label = Качество
ui-options-misc-label = Разное
ui-options-interface-label = Интерфейс
ui-options-auto-fill-highlights = Автозаполнение подсветки информацией персонажа
ui-options-highlights-color = Цвет подсветки:
ui-options-highlights-color-example = Это подсвеченный текст!

## Graphics menu

ui-options-show-held-item = Показать удерживаемый элемент рядом с курсором
ui-options-show-combat-mode-indicators = Показать индикатор боевого режима рядом с курсором
ui-options-opaque-storage-window = Непрозрачность окна хранилища
ui-options-show-ooc-patron-color = Цветной ник в OOC для патронов с Patreon
ui-options-show-looc-on-head = Показывать LOOC-чат над головами персонажей
ui-options-fancy-speech = Показывать имена в облачках с текстом
ui-options-fancy-name-background = Добавить фон облачкам с текстом
ui-options-vsync = Вертикальная синхронизация
ui-options-fullscreen = Полный экран
ui-options-lighting-label = Качество освещения:
ui-options-lighting-very-low = Очень низкое
ui-options-lighting-low = Низкое
ui-options-lighting-medium = Среднее
ui-options-lighting-high = Высокое
ui-options-scale-label = Масштаб UI:
ui-options-scale-auto = Автоматическое ({ TOSTRING($scale, "P0") })
ui-options-scale-75 = 75%
ui-options-scale-100 = 100%
ui-options-scale-125 = 125%
ui-options-scale-150 = 150%
ui-options-scale-175 = 175%
ui-options-scale-200 = 200%
ui-options-hud-theme = Тема HUD:
ui-options-hud-theme-default = По умолчанию
ui-options-hud-theme-plasmafire = Плазма
ui-options-hud-theme-slimecore = Слаймкор
ui-options-hud-theme-clockwork = Механизм
ui-options-hud-theme-retro = Ретро
ui-options-hud-theme-minimalist = Минимализм
ui-options-hud-theme-ashen = Пепел
ui-options-hud-layout-default = Стандартный
ui-options-hud-background-parallax = Параллакс
ui-options-hud-background-image = Фон
ui-options-hud-layout-separated = Разделённый
ui-options-vp-stretch = Растянуть изображение для соответствия окну игры
ui-options-vp-scale = Фиксированный масштаб окна игры:
ui-options-vp-scale-value = x{ $scale }
ui-options-vp-integer-scaling = Использовать целочисленное масштабирование (может вызывать появление чёрных полос/обрезания)
ui-options-filter-label = Фильтр масштабирования:
ui-options-filter-nearest = Ближайший (без сглаживания)
ui-options-filter-bilinear = Билинейный (сглаживание)
ui-options-vp-integer-scaling-tooltip =
    Если эта опция включена, область просмотра будет масштабироваться,
    используя целочисленное значение при определённых разрешениях. Хотя это и
    приводит к чётким текстурам, это часто означает, что сверху/снизу экрана будут
    чёрные полосы или что часть окна не будет видна.
ui-options-vp-vertical-fit = Подгон окна просмотра по вертикали
ui-options-vp-vertical-fit-tooltip =
    Когда функция включена, основное окно просмотра не будет учитывать горизонтальную ось
    при подгонке под ваш экран. Если ваш экран меньше, чем окно просмотра,
    то это приведёт к его обрезанию по горизонтальной оси.
ui-options-vp-low-res = Изображение низкого разрешения
ui-options-ambient-occlusion = Show Ambient Occlusion
ui-options-parallax-low-quality = Низкокачественный параллакс (фон)
ui-options-fps-counter = Показать счётчик FPS
ui-options-vp-width = Ширина окна игры:
ui-options-hud-layout = Тип HUD:
ui-options-background-hud-layout = Тип фона лобби:

## Controls menu

ui-options-binds-reset-all = Сбросить ВСЕ привязки
ui-options-binds-explanation = ЛКМ — изменить кнопку, ПКМ — убрать кнопку
ui-options-unbound = Пусто
ui-options-bind-reset = Сбросить
ui-options-key-prompt = Нажмите кнопку...
ui-options-header-movement = Перемещение
ui-options-header-camera = Камера
ui-options-header-interaction-basic = Базовые взаимодействия
ui-options-header-interaction-adv = Продвинутые взаимодействия
ui-options-header-ui = Интерфейс
ui-options-header-misc = Разное
ui-options-header-hotbar = Хотбар
ui-options-header-shuttle = Шаттл
ui-options-header-map-editor = Редактор карт
ui-options-header-dev = Разработка
ui-options-header-text-cursor = Текстовый курсор
ui-options-header-text-cursor-select = Выделение текста
ui-options-header-text-edit = Редактирование текста
ui-options-header-text-chat = Чат
ui-options-header-text-other = Прочий ввод текста
ui-options-header-general = Основное
ui-options-hotkey-keymap = Использовать клавиши QWERTY (США)
ui-options-hotkey-toggle-walk = Переключить ходьбу
ui-options-function-move-up = Двигаться вверх
ui-options-function-move-left = Двигаться налево
ui-options-function-move-down = Двигаться вниз
ui-options-function-move-right = Двигаться направо
ui-options-function-toggle-knockdown = Переключить ползание
ui-options-function-walk = Ходьба
ui-options-function-camera-rotate-left = Повернуть налево
ui-options-function-camera-rotate-right = Повернуть направо
ui-options-function-camera-reset = Сбросить камеру
ui-options-function-zoom-in = Приблизить
ui-options-function-zoom-out = Отдалить
ui-options-function-reset-zoom = Сбросить
ui-options-function-use = Использовать
ui-options-function-use-secondary = Использовать вторично
ui-options-function-alt-use = Альтернативное использование
ui-options-function-wide-attack = Размашистая атака
ui-options-function-activate-item-in-hand = Использовать предмет в руке
ui-options-function-alt-activate-item-in-hand = Альтернативно использовать предмет в руке
ui-options-function-activate-item-in-world = Использовать предмет в мире
ui-options-function-alt-activate-item-in-world = Альтернативно использовать предмет в мире
ui-options-function-drop = Положить предмет
ui-options-function-examine-entity = Осмотреть
ui-options-function-swap-hands-reverse = Поменять руки (противоположное направление)
ui-options-function-swap-hands = Поменять руки
ui-options-function-move-stored-item = Переместить хранящийся объект
ui-options-function-rotate-stored-item = Повернуть хранящийся объект
ui-options-function-save-item-location = Сохранить расположение объекта
ui-options-static-storage-ui = Закрепить интерфейс хранилища на хотбаре
ui-options-function-smart-equip-backpack = Умная экипировка в рюкзак
ui-options-function-smart-equip-belt = Умная экипировка на пояс
ui-options-function-open-backpack = Открыть рюкзак
ui-options-function-open-belt = Открыть пояс
ui-options-function-throw-item-in-hand = Бросить предмет
ui-options-function-try-pull-object = Тянуть объект
ui-options-function-move-pulled-object = Тянуть объект в сторону
ui-options-function-release-pulled-object = Перестать тянуть объект
ui-options-function-point = Указать на что-либо
ui-options-function-rotate-object-clockwise = Повернуть по часовой стрелке
ui-options-function-rotate-object-counterclockwise = Повернуть против часовой стрелки
ui-options-function-flip-object = Перевернуть
ui-options-function-focus-chat-input-window = Писать в чат
ui-options-function-focus-local-chat-window = Писать в чат (IC)
ui-options-function-focus-emote = Писать в чат (Emote)
ui-options-function-focus-whisper-chat-window = Писать в чат (Шёпот)
ui-options-function-focus-radio-window = Писать в чат (Радио)
ui-options-function-focus-looc-window = Писать в чат (LOOC)
ui-options-function-focus-ooc-window = Писать в чат (OOC)
ui-options-function-focus-admin-chat-window = Писать в чат (Админ)
ui-options-function-focus-dead-chat-window = Писать в чат (Мёртвые)
ui-options-function-focus-console-chat-window = Писать в чат (Консоль)
ui-options-function-cycle-chat-channel-forward = Переключение каналов чата (Вперёд)
ui-options-function-cycle-chat-channel-backward = Переключение каналов чата (Назад)
ui-options-function-open-character-menu = Открыть меню персонажа
ui-options-function-open-context-menu = Открыть контекстное меню
ui-options-function-open-crafting-menu = Открыть меню строительства
ui-options-function-open-inventory-menu = Открыть снаряжение
ui-options-function-open-a-help = Открыть админ помощь
ui-options-function-open-abilities-menu = Открыть меню действий
ui-options-function-open-emotes-menu = Открыть меню эмоций
ui-options-function-toggle-round-end-summary-window = Переключить окно итогов раунда
ui-options-function-open-entity-spawn-window = Открыть меню спавна сущностей
ui-options-function-open-sandbox-window = Открыть меню песочницы
ui-options-function-open-tile-spawn-window = Открыть меню спавна тайлов
ui-options-function-open-decal-spawn-window = Открыть меню спавна декалей
ui-options-function-open-admin-menu = Открыть админ меню
ui-options-function-open-guidebook = Открыть руководство
ui-options-function-window-close-all = Закрыть все окна
ui-options-function-window-close-recent = Закрыть текущее окно
ui-options-function-show-escape-menu = Переключить игровое меню
ui-options-function-escape-context = Закрыть текущее окно или переключить игровое меню
ui-options-function-take-screenshot = Сделать скриншот
ui-options-function-take-screenshot-no-ui = Сделать скриншот (без интерфейса)
ui-options-function-toggle-fullscreen = Переключить полноэкранный режим
ui-options-function-editor-place-object = Разместить объект
ui-options-function-editor-cancel-place = Отменить размещение
ui-options-function-editor-grid-place = Размещать в сетке
ui-options-function-editor-line-place = Размещать в линию
ui-options-function-editor-rotate-object = Повернуть
ui-options-function-editor-flip-object = Перевернуть
ui-options-function-editor-copy-object = Копировать
ui-options-function-show-debug-console = Открыть консоль
ui-options-function-show-debug-monitors = Показать дебаг информацию
ui-options-function-inspect-entity = Изучить сущность
ui-options-function-hide-ui = Спрятать интерфейс
ui-options-function-hotbar1 = 1 слот хотбара
ui-options-function-hotbar2 = 2 слот хотбара
ui-options-function-hotbar3 = 3 слот хотбара
ui-options-function-hotbar4 = 4 слот хотбара
ui-options-function-hotbar5 = 5 слот хотбара
ui-options-function-hotbar6 = 6 слот хотбара
ui-options-function-hotbar7 = 7 слот хотбара
ui-options-function-hotbar8 = 8 слот хотбара
ui-options-function-hotbar9 = 9 слот хотбара
ui-options-function-hotbar-shift1 = Слот хотбара Shift+1
ui-options-function-hotbar-shift2 = Слот хотбара Shift+2
ui-options-function-hotbar-shift3 = Слот хотбара Shift+3
ui-options-function-hotbar-shift4 = Слот хотбара Shift+4
ui-options-function-hotbar-shift5 = Слот хотбара Shift+5
ui-options-function-hotbar-shift6 = Слот хотбара Shift+6
ui-options-function-hotbar-shift7 = Слот хотбара Shift+7
ui-options-function-hotbar-shift8 = Слот хотбара Shift+8
ui-options-function-hotbar-shift9 = Слот хотбара Shift+9
ui-options-function-hotbar-shift0 = Слот хотбара Shift+0
ui-options-function-hotbar0 = 0 слот хотбара
ui-options-function-loadout1 = 1 страница хотбара
ui-options-function-loadout2 = 2 страница хотбара
ui-options-function-loadout3 = 3 страница хотбара
ui-options-function-loadout4 = 4 страница хотбара
ui-options-function-loadout5 = 5 страница хотбара
ui-options-function-loadout6 = 6 страница хотбара
ui-options-function-loadout7 = 7 страница хотбара
ui-options-function-loadout8 = 8 страница хотбара
ui-options-function-loadout9 = 9 страница хотбара
ui-options-function-loadoutshift1 = Страница хотбара Shift+1
ui-options-function-loadoutshift2 = Страница хотбара Shift+2
ui-options-function-loadoutshift3 = Страница хотбара Shift+3
ui-options-function-loadoutshift4 = Страница хотбара Shift+4
ui-options-function-loadoutshift5 = Страница хотбара Shift+5
ui-options-function-loadoutshift6 = Страница хотбара Shift+6
ui-options-function-loadoutshift7 = Страница хотбара Shift+7
ui-options-function-loadoutshift8 = Страница хотбара Shift+8
ui-options-function-loadoutshift9 = Страница хотбара Shift+9
ui-options-function-loadoutshift0 = Страница хотбара Shift+0
ui-options-function-loadout0 = 0 страница хотбара
ui-options-function-shuttle-strafe-up = Стрейф вверх
ui-options-function-shuttle-strafe-right = Стрейф вправо
ui-options-function-shuttle-strafe-left = Стрейф влево
ui-options-function-shuttle-strafe-down = Стрейф вниз
ui-options-function-shuttle-rotate-left = Поворот налево
ui-options-function-shuttle-rotate-right = Поворот направо
ui-options-function-text-cursor-left = Передвинуть курсор влево
ui-options-function-text-cursor-right = Передвинуть курсор вправо
ui-options-function-text-cursor-up = Передвинуть курсор вверх
ui-options-function-text-cursor-down = Передвинуть курсор вниз
ui-options-function-text-cursor-word-left = Передвинуть курсор влево на слово
ui-options-function-text-cursor-word-right = Передвинуть курсор вправо на слово
ui-options-function-text-cursor-begin = Передвинуть курсор в начало
ui-options-function-text-cursor-end = Передвинуть курсор в конец
ui-options-function-text-cursor-select = Выделить текст
ui-options-function-text-cursor-select-left = Расширить выделение влево
ui-options-function-text-cursor-select-right = Расширить выделение вправо
ui-options-function-text-cursor-select-up = Расширить выделение вверх
ui-options-function-text-cursor-select-down = Расширить выделение вниз
ui-options-function-text-cursor-select-word-left = Расширить выделение влево на слово
ui-options-function-text-cursor-select-word-right = Расширить выделение вправо на слово
ui-options-function-text-cursor-select-begin = Расширить выделение до начала
ui-options-function-text-cursor-select-end = Расширить выделение до конца
ui-options-function-text-backspace = Стереть
ui-options-function-text-delete = Стереть спереди
ui-options-function-text-word-backspace = Стереть слово
ui-options-function-text-word-delete = Стереть слово спереди
ui-options-function-text-newline = Новая строка
ui-options-function-text-submit = Подтвердить
ui-options-function-multiline-text-submit = Подтвердить несколько строк
ui-options-function-text-select-all = Выделить всё
ui-options-function-text-copy = Копировать
ui-options-function-text-cut = Вырезать
ui-options-function-text-paste = Вставить
ui-options-function-text-history-prev = Предыдущее с истории
ui-options-function-text-history-next = Следующее с истории
ui-options-function-text-release-focus = Убрать фокус
ui-options-function-text-scroll-to-bottom = Пролистать вниз
ui-options-function-text-tab-complete = Завершение при помощи Tab
ui-options-function-text-complete-next = Продолжить следующее
ui-options-function-text-complete-prev = Продолжить прошлое
ui-options-function-shuttle-brake = Торможение
ui-options-net-interp-ratio = Сетевое сглаживание
ui-options-net-predict = Предугадывание на стороне клиента
ui-options-net-interp-ratio-tooltip =
    Увеличение этого параметра, как правило, делает игру
    более устойчивой к потере пакетов, однако при этом
    это так же добавляет немного больше задержки и
    требует от клиента предсказывать больше будущих тиков.
ui-options-net-predict-tick-bias = Погрешность тиков предугадывания
ui-options-net-predict-tick-bias-tooltip =
    Увеличение этого параметра, как правило, делает игру более устойчивой
    к потере пакетов между клиентом и сервером, однако при этом
    немного возрастает задержка, и клиенту требуется предугадывать
    больше будущих тиков
ui-options-net-pvs-spawn = Лимит появление PVS сущностей
ui-options-net-pvs-spawn-tooltip =
    Ограничение частоты отправки новых появившихся сущностей сервером на клиент.
    Снижение этого параметра может помочь уменьшить "захлёбывания",
    вызываемые спавном сущностей, но может привести к их резкому появлению.
ui-options-net-pvs-entry = Лимит PVS сущностей
ui-options-net-pvs-entry-tooltip =
    Ограничение частоты отправки новых видимых сущностей сервером на клиент.
    Снижение этого параметра может помочь уменьшить "захлёбывания",
    вызываемые спавном сущностей, но может привести к их резкому появлению.
ui-options-net-pvs-leave = Частота удаления PVS
ui-options-net-pvs-leave-tooltip =
    Ограничение частоты, с которой клиент будет удалять
    сущности вне поля зрения. Снижение этого параметра может помочь
    уменьшить "захлёбывания" при ходьбе, но иногда может
    привести к неправильным предугадываниям и другим проблемам.
cmd-options-desc = Открывает меню опций, опционально с конкретно выбранной вкладкой.
cmd-options-help = Использование: options [tab]
ui-options-enable-color-name = Цветные имена персонажей
ui-options-colorblind-friendly = Режим для дальтоников
ui-options-accessability-header-visuals = Изображение
ui-options-accessability-header-content = Содержимое
ui-options-reduced-motion = Снижение интенсивности визуальных эффектов
ui-options-chat-window-opacity = Прозрачность окна чата
ui-options-chat-window-opacity-percent = { TOSTRING($opacity, "P0") }
ui-options-screen-shake-intensity = Интенсивность дрожания экрана
ui-options-screen-shake-percent = { TOSTRING($intensity, "P0") }
ui-options-speech-bubble-text-opacity = Непрозрачность текста речевого пузыря
ui-options-speech-bubble-speaker-opacity = Непрозрачность диктора речевого пузыря
ui-options-speech-bubble-background-opacity = Непрозрачность фона речевого пузыря
ui-options-censor-nudity = Цензура обнажённых персонажей
ui-options-enable-classic-overlay = Вернуть антаг-оверлей в классический режим
ui-options-admin-player-panel = Список персонажей в админ меню
ui-options-admin-player-tab-symbol-setting = Показ символа антагониста
ui-options-admin-player-tab-symbol-setting-off = Нет антаг символа
ui-options-admin-player-tab-symbol-setting-basic = Показать стандартный антаг символ
ui-options-admin-player-tab-symbol-setting-specific = Показать специфичный антаг символ
ui-options-admin-player-tab-role-setting = Настройки отображения роли
ui-options-admin-player-tab-role-setting-roletype = Показывать тип роли
ui-options-admin-player-tab-role-setting-subtype = Показывать подтип
ui-options-admin-player-tab-role-setting-roletypesubtype = Тип и подтип роли
ui-options-admin-player-tab-role-setting-subtyperoletype = Подтип и тип роли
ui-options-admin-player-tab-color-setting = Настройки цвета
ui-options-admin-player-tab-color-setting-off = Я ненавижу цвета!
ui-options-admin-player-tab-color-setting-character = Окрашивать имена всех ролей антагонистов
ui-options-admin-player-tab-color-setting-roletype = Окрашивать роли всех типов
ui-options-admin-player-tab-color-setting-both = Окрашивать оба
ui-options-admin-logs-title = Админ логи
ui-options-admin-logs-highlight-color = Цвет выделения
ui-options-admin-playerlist-separate-symbols = Показывать отдельные символы для каждого типа антагониста
ui-options-admin-overlay-antag-format = Стиль иконки антагониста
ui-options-admin-overlay-antag-format-binary = Показывать статус антагониста
ui-options-admin-overlay-antag-format-roletype = Показать тип роли
ui-options-admin-overlay-antag-format-subtype = Показать субтип роли
ui-options-admin-overlay-antag-symbol = Стиль символа антагониста
ui-options-admin-overlay-antag-symbol-off = Без символа
ui-options-admin-overlay-antag-symbol-basic = Показывать стандартный символ
ui-options-admin-overlay-antag-symbol-specific = Показывать специфический символ
ui-options-admin-enable-overlay-playtime = Показывать общее игровое время
ui-options-admin-enable-overlay-starting-job = Показать стартовую профессию
ui-options-admin-overlay-merge-distance = Дальность сложения оверлея
ui-options-admin-overlay-ghost-fade-distance = Диапазон затухания наложения призрака от мыши
ui-options-admin-overlay-ghost-hide-distance = Диапазон скрытия наложения призрака от мыши
ui-options-admin-playerlist-character-color = Цветные имена антагонистов
ui-options-admin-playerlist-roletype-color = Цветные типы ролей
ui-options-admin-overlay-title = Админ оверлей
ui-options-enable-overlay-symbols = Добавить символ антага к тексту
ui-options-enable-overlay-playtime = Отображать наигранное время
ui-options-enable-overlay-starting-job = Показывать начальную должность
ui-options-overlay-merge-distance = Дальность сложения оверлея
ui-options-overlay-ghost-fade-distance = Диапазон затухания наложения призрака от мыши
ui-options-overlay-ghost-hide-distance = Диапазон скрытия наложения призрака от мыши
