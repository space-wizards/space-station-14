## General stuff

ui-options-title = Игровые настройки
ui-options-tab-graphics = Графика
ui-options-tab-controls = Управление
ui-options-tab-audio = Аудио
ui-options-tab-network = Сеть
ui-options-apply = Применить
ui-options-reset-all = Сбросить всё
ui-options-default = По-умолчанию

## Audio menu

ui-options-master-volume = Основная громкость:
ui-options-midi-volume = MIDI (Инструменты) громкость:
ui-options-ambience-volume = Громкость окружения:
ui-options-lobby-volume = Громкость лобби и окончания раунда:
ui-options-ambience-max-sounds = Кол-во одновременных звуков окружения:
ui-options-lobby-music = Музыка в лобби
ui-options-restart-sounds = Звуки перезапуска раунда
ui-options-event-music = Музыка событий
ui-options-admin-sounds = Музыка админов
ui-options-station-ambience = Эмбиент станции
ui-options-space-ambience = Эмбиент космоса
ui-options-volume-label = Громкость
ui-options-volume-percent = { TOSTRING($volume, "P0") }

## Graphics menu

ui-options-show-held-item = Показать удерживаемый элемент рядом с курсором?
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
ui-options-hud-theme-modernized = Модернизированная
ui-options-hud-theme-classic = Классическая
ui-options-vp-stretch = Растянуть изображение для соответствия окну игры
ui-options-vp-scale = Фиксированный масштаб окна игры: x{ $scale }
ui-options-vp-integer-scaling = Prefer integer scaling (might cause black bars/clipping)
ui-options-vp-integer-scaling-tooltip =
    If this option is enabled, the viewport will be scaled using an integer value
    at specific resolutions. While this results in crisp textures, it also often
    means that black bars appear at the top/bottom of the screen or that part
    of the viewport is not visible.
ui-options-vp-low-res = Изображение низкого разрешения
ui-options-parallax-low-quality = Низкокачественный параллакс (фон)
ui-options-fps-counter = Показать счетчик FPS
ui-options-vp-width = Ширина окна игры: { $width }
ui-options-hud-layout = Тип HUD:

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
ui-options-header-general = Основное
ui-options-hotkey-keymap = Использовать клавиши QWERTY (США)
ui-options-function-move-up = Двигаться вверх
ui-options-function-move-left = Двигаться налево
ui-options-function-move-down = Двигаться вниз
ui-options-function-move-right = Двигаться направо
ui-options-function-walk = Идти
ui-options-function-camera-rotate-left = Повернуть налево
ui-options-function-camera-rotate-right = Повернуть направо
ui-options-function-camera-reset = Сбросить камеру
ui-options-function-use = Использовать
ui-options-function-use-secondary = Use secondary
ui-options-function-alt-use = Альтернативное использование
ui-options-function-wide-attack = Размашистая атака
ui-options-function-activate-item-in-hand = Использовать предмет в руке
ui-options-function-alt-activate-item-in-hand = Альтернативно использовать предмет в руке
ui-options-function-activate-item-in-world = Использовать предмет в мире
ui-options-function-alt-activate-item-in-world = Альтернативно использовать предмет в мире
ui-options-function-drop = Бросить предмет
ui-options-function-examine-entity = Изучить
ui-options-function-swap-hands = Поменять руки
ui-options-function-smart-equip-backpack = Умная экипировка в рюкзак
ui-options-function-smart-equip-belt = Умная экипировка на пояс
ui-options-function-throw-item-in-hand = Бросить предмет
ui-options-function-try-pull-object = Тянуть объект
ui-options-function-move-pulled-object = Тянуть объект в сторону
ui-options-function-release-pulled-object = Перестать тянуть объект
ui-options-function-point = Указать на что-либо
ui-options-function-focus-chat-input-window = Писать в чат
ui-options-function-focus-local-chat-window = Писать в чат (IC)
ui-options-function-focus-whisper-chat-window = Писать в чат (Шёпот)
ui-options-function-focus-radio-window = Писать в чат (Радио)
ui-options-function-focus-ooc-window = Писать в чат (OOC)
ui-options-function-focus-admin-chat-window = Писать в чат (Админ)
ui-options-function-focus-dead-chat-window = Писать в чат (Мертвые)
ui-options-function-focus-console-chat-window = Писать в чат (Консоль)
ui-options-function-cycle-chat-channel-forward = Переключение каналов чата (Вперёд)
ui-options-function-cycle-chat-channel-backward = Переключение каналов чата (Назад)
ui-options-function-open-character-menu = Открыть меню персонажа
ui-options-function-open-context-menu = Открыть контекстное меню
ui-options-function-open-crafting-menu = Открыть меню строительства
ui-options-function-open-inventory-menu = Открыть снаряжение
ui-options-function-open-a-help = Открыть админ помощь
ui-options-function-open-abilities-menu = Открыть меню действий
ui-options-function-open-entity-spawn-window = Открыть меню спавна сущностей
ui-options-function-open-sandbox-window = Открыть меню песочницы
ui-options-function-open-tile-spawn-window = Открыть меню спавна тайлов
ui-options-function-open-decal-spawn-window = Открыть меню спавна декалей
ui-options-function-open-admin-menu = Открыть админ меню
ui-options-function-take-screenshot = Сделать скриншот
ui-options-function-take-screenshot-no-ui = Сделать скриншот (без интерфейса)
ui-options-function-editor-place-object = Разместить объект
ui-options-function-editor-cancel-place = Отменить размещение
ui-options-function-editor-grid-place = Размещать в сетке
ui-options-function-editor-line-place = Размещать в линию
ui-options-function-editor-rotate-object = Повернуть
ui-options-function-editor-copy-object = Копировать
ui-options-function-open-abilities-menu = Открыть меню действий
ui-options-function-show-debug-console = Открыть консоль
ui-options-function-show-debug-monitors = Показать дебаг информацию
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
ui-options-function-loadout0 = 0 страница хотбара
ui-options-function-shuttle-strafe-up = Стрейф вверх
ui-options-function-shuttle-strafe-right = Стрейф вправо
ui-options-function-shuttle-strafe-left = Стрейф влево
ui-options-function-shuttle-strafe-down = Стрейф вниз
ui-options-function-shuttle-rotate-left = Поворот налево
ui-options-function-shuttle-rotate-right = Поворот направо
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
    Снижение этого параметра может помочь уменьшить "захлебывания",
    вызываемые спавном сущностей, но может привести к их резкому появлению.
ui-options-net-pvs-entry = Лимит PVS сущностей
ui-options-net-pvs-entry-tooltip =
    Ограничение частоты отправки новых видимых сущностей сервером на клиент. 
    Снижение этого параметра может помочь уменьшить "захлебывания",
    вызываемые спавном сущностей, но может привести к их резкому появлению.
ui-options-net-pvs-leave = Частота удаления PVS
ui-options-net-pvs-leave-tooltip =
    Ограничение частоты, с которой клиент будет удалять 
    сущности вне поля зрения. Снижение этого параметра может помочь
    уменьшить "захлебывания" при ходьбе, но иногда может 
    привести к неправильным предугадываниям и другим проблемам.
