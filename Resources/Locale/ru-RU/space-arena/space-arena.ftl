space-arena-mode-unknown = Неизвестный режим
space-arena-mode-team-deathmatch = Командный бой насмерть
space-arena-mode-drawing = Рисование
space-arena-lobby-unknown-host = Неизвестный игрок
space-arena-lobby-player-joined = К лобби присоединился игрок {$player}. Игроков: {$players}/{$max}.

space-arena-match-countdown = Бой начнётся через {$seconds} сек.!
space-arena-match-fight-start = В бой!
space-arena-match-victory = Вы победили!
space-arena-match-defeat = Вы проиграли!
space-arena-match-draw = Бой завершился без победителя.

space-arena-lobby-window-title = SpaceArena
space-arena-lobby-heading = Лобби игроков
space-arena-lobby-description = Создайте комнату или присоединитесь к матчу другого игрока. Создатель запускает игру, когда все готовы.
space-arena-lobby-create-heading = Создать лобби
space-arena-lobby-mode-label = Режим
space-arena-lobby-arena-label = Арена
space-arena-lobby-create-button = Создать
space-arena-lobby-arena-option = {$arena} · {$format}
space-arena-lobby-weapon-preview-tooltip = Основное оружие арены
space-arena-lobby-membership-none = Вы не состоите в лобби.
space-arena-lobby-membership-joined = Вы присоединились к лобби.
space-arena-lobby-membership-active = Матч идёт. Изменение лобби недоступно.
space-arena-lobby-membership-spectating = Вы наблюдаете за матчем.
space-arena-lobby-leave-button = Покинуть лобби
space-arena-lobby-list-heading = Лобби и активные матчи
space-arena-lobby-list-empty = Лобби и активных матчей нет. Создайте первый!
space-arena-lobby-room-title = {$host} · {$mode}
space-arena-lobby-room-details = {$arena} · {$players}/{$max} игроков · минимум {$min} · {$state}
space-arena-lobby-start-button = Начать матч
space-arena-lobby-start-disabled = Требуется минимум {$min} игроков.
space-arena-lobby-you-joined = Вы в лобби
space-arena-lobby-join-button = Присоединиться
space-arena-lobby-spectate-button = Наблюдать
space-arena-lobby-you-spectate = Вы наблюдаете
space-arena-lobby-state-waiting = Ожидание
space-arena-lobby-state-preparing = Подготовка
space-arena-lobby-state-countdown = Отсчёт
space-arena-lobby-state-active = Идёт матч
space-arena-lobby-state-finishing = Завершение
space-arena-hud-button = АРЕНЫ
space-arena-hud-button-tooltip = Открыть лобби SpaceArena
space-arena-hud-return-to-hub-button = ВЕРНУТЬСЯ В ХАБ
space-arena-hud-return-to-hub-tooltip = Покинуть текущее занятие и вернуться в хаб SpaceArena

ent-ComputerSpaceArenaLobby = Терминал лобби SpaceArena
    .desc = Просматривайте комнаты игроков, присоединяйтесь к матчам или создавайте свои.
ent-SpaceArenaLobbyComputerCircuitboard = Плата терминала лобби SpaceArena
    .desc = Печатная плата для терминала лобби SpaceArena.

cmd-arena-create-desc = Создаёт ожидающий матч SpaceArena.
cmd-arena-create-help = Использование: arena_create <прототип режима> <прототип карты арены>
cmd-arena-create-mode-hint = Прототип режима матча
cmd-arena-create-map-hint = Прототип карты арены
cmd-arena-create-failed = Не удалось создать режим {$mode} на арене {$arena}.
cmd-arena-create-success = Матч {$match} создан.

cmd-arena-join-desc = Добавляет выполнившего команду администратора в ожидающий матч SpaceArena.
cmd-arena-join-help = Использование: arena_join <сущность матча>
cmd-arena-player-required = Для этой команды требуется подключённый игрок.
cmd-arena-join-failed = Не удалось присоединиться к этому матчу.
cmd-arena-join-success = Вы присоединились к ожидающему матчу.

cmd-arena-start-desc = Запускает ожидающий матч SpaceArena.
cmd-arena-start-help = Использование: arena_start <сущность матча>
cmd-arena-start-failed = Не удалось начать матч. Проверьте его состояние и минимальное число игроков.
cmd-arena-start-success = Запуск матча начат.

cmd-arena-finish-desc = Завершает активный матч SpaceArena.
cmd-arena-finish-help = Использование: arena_finish <сущность матча>
cmd-arena-finish-failed = Не удалось завершить матч.
cmd-arena-finish-success = Матч завершается.

cmd-arena-leave-desc = Покидает текущий матч SpaceArena.
cmd-arena-leave-help = Использование: arena_leave
cmd-arena-leave-failed = Вы не участвуете в матче SpaceArena.
cmd-arena-leave-success = Вы покинули матч.

cmd-arena-list-desc = Выводит список текущих матчей SpaceArena.
cmd-arena-list-help = Использование: arena_list
cmd-arena-list-empty = Матчей SpaceArena нет.
cmd-arena-list-entry = [{$match}] {$mode}: {$state}, {$players}/{$capacity} игроков
space-arena-preset-title = SpaceArena
space-arena-preset-description = Социальный хаб с матчами на аренах и мини-играми, создаваемыми игроками.
