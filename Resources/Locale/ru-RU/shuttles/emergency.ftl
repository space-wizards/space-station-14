# Commands
## Delay shuttle round end
emergency-shuttle-command-round-desc = Останавливает таймер окончания раунда, когда аварийный шаттл покидает гиперпространство.
emergency-shuttle-command-round-yes = Раунд продлён.
emergency-shuttle-command-round-no = Невозможно продлить окончание раунда.

## Dock emergency shuttle
emergency-shuttle-command-dock-desc = Вызывает спасательный шаттл и пристыковывает его к станции... если это возможно.

## Launch emergency shuttle
emergency-shuttle-command-launch-desc = Досрочно запускает аварийный шаттл, если это возможно.

# Emergency shuttle
emergency-shuttle-left = Аварийный шаттл покинул станцию. Расчетное время прибытия шаттла на станцию ЦентКома - { $transitTime } секунд.
emergency-shuttle-launch-time = Аварийный шаттл будет запущен через { $consoleAccumulator } секунд.
emergency-shuttle-docked = Аварийный шаттл пристыковался к станции, сторона: { $direction }. Он улетит через { $time } секунд.
emergency-shuttle-good-luck = Аварийный шаттл не может найти станцию. Удачи.
emergency-shuttle-nearby = Аварийный шаттл не может найти подходящий стыковочный шлюз. Он дрейфует неподалёку.

# Emergency shuttle console popup / announcement
emergency-shuttle-console-auth-left =
    { $remaining } { $remaining ->
        [one] авторизация осталась
        [few] авторизации остались
       *[other] авторизации остались
    } для досрочного запуска шаттла.
emergency-shuttle-console-auth-revoked =
    Авторизации на досрочный запуск шаттла отозваны, { $remaining } { $remaining ->
        [one] авторизация необходима
        [few] авторизации необходимы
       *[other] авторизации необходимы
    }.
emergency-shuttle-console-denied = Доступ запрещён

# UI
emergency-shuttle-ui-engines = ДВИГАТЕЛИ:
emergency-shuttle-ui-idle = Простой
emergency-shuttle-ui-repeal-all = Повторить всё
emergency-shuttle-ui-early-authorize = Разрешение на досрочный запуск
emergency-shuttle-ui-authorize = АВТОРИЗОВАТЬСЯ
emergency-shuttle-ui-repeal = ПОВТОРИТЬ
emergency-shuttle-ui-authorizations = Авторизации
emergency-shuttle-ui-remaining = Осталось: { $remaining }
