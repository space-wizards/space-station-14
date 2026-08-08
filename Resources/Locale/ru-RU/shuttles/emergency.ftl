# Commands

## Delay shuttle round end

cmd-delayroundend-desc = Приостанавливает или изменяет таймер до завершения раунда, а после манифеста — до перезапуска.
cmd-delayroundend-help =
    Использование: delayroundend [секунды]
    Положительное значение добавляет время, отрицательное — убавляет. Без аргумента приостанавливает или возобновляет таймер.
cmd-delayroundend-invalid-seconds = { $value } не является допустимым количеством секунд.
cmd-delayroundend-paused =
    { $restart ->
        [true] Перезапуск раунда приостановлен.
       *[false] Конец раунда приостановлен.
    }
cmd-delayroundend-resumed =
    { $restart ->
        [true] Перезапуск раунда возобновлён.
       *[false] Конец раунда возобновлён.
    }
cmd-delayroundend-extended =
    { $restart ->
        [true] Перезапуск раунда отложен на { $seconds } секунд.
       *[false] Конец раунда продлён на { $seconds } секунд.
    }
cmd-delayroundend-shortened =
    { $restart ->
        [true] Перезапуск раунда приближен на { $seconds } секунд.
       *[false] Конец раунда сокращён на { $seconds } секунд.
    }
cmd-delayroundend-no-timer = Нет активного таймера завершения или перезапуска раунда.

## Dock emergency shuttle

cmd-dockemergencyshuttle-desc = Вызывает спасательный шаттл и пристыковывает его к станции... если это возможно.
cmd-dockemergencyshuttle-help = Использование: dockemergencyshuttle

## Launch emergency shuttle

cmd-launchemergencyshuttle-desc = Досрочно запускает эвакуационный шаттл, если это возможно.
cmd-launchemergencyshuttle-help = Использование: launchemergencyshuttle
# Emergency shuttle
emergency-shuttle-left = Эвакуационный шаттл покинул станцию. Расчётное время прибытия шаттла на станцию Центкома — { $transitTime } секунд.
emergency-shuttle-launch-time = Эвакуационный шаттл будет запущен через { $consoleAccumulator } секунд.
emergency-shuttle-docked = Эвакуационный шаттл пристыковался к станции { $location }, направление: { $direction }. Он улетит через { $time } секунд.{ $extended }
emergency-shuttle-good-luck = Эвакуационный шаттл не может найти станцию. Удачи.
emergency-shuttle-nearby = Эвакуационный шаттл не может найти подходящий стыковочный шлюз. Он дрейфует около станции, { $location }, направление: { $direction }. Он улетит через { $time } секунд.{ $extended }
emergency-shuttle-extended = { " " }Время до запуска было продлено в связи с непредвиденными обстоятельствами.
# Emergency shuttle console popup / announcement
emergency-shuttle-console-no-early-launches = Досрочный запуск отключён
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
emergency-shuttle-console-window-title = Консоль эвакуационного шаттла
emergency-shuttle-ui-engines = ДВИГАТЕЛИ:
emergency-shuttle-ui-idle = Простой
emergency-shuttle-ui-status-title = СОСТОЯНИЕ ШАТТЛА
emergency-shuttle-ui-status-waiting = Ожидание запуска
emergency-shuttle-ui-status-early-disabled = Штатный маршрут
emergency-shuttle-ui-status-countdown = Старт через { $time }
emergency-shuttle-ui-route = Маршрут:
emergency-shuttle-ui-route-standard = Центральное Командование
emergency-shuttle-ui-route-raider-outpost = Аванпост рейдеров Синдиката
emergency-shuttle-ui-repeal-all = Повторить всё
emergency-shuttle-ui-early-authorize = Разрешение на досрочный запуск
emergency-shuttle-ui-authorize = АВТОРИЗОВАТЬСЯ
emergency-shuttle-ui-repeal = ПОВТОРИТЬ
emergency-shuttle-ui-authorizations = Авторизации
emergency-shuttle-ui-remaining = Осталось: { $remaining }
emergency-shuttle-console-hijack-denied = Активный контракт на угон не найден.
emergency-shuttle-console-hijack-already-started = Последовательность угона уже активна.
emergency-shuttle-console-hijack-already-complete = Шаттл уже перехвачен.
emergency-shuttle-console-hijack-announcer = Автоматические Системы Станции
emergency-shuttle-console-hijack-started = Внимание всему Офицерскому Составу Объекта! Зафиксирован неавторизованный доступ к системам эвакуационного шаттла! Фиксируется попытка изменения блюспейс-пути, точное местоположение - неизвестно. Службе Безопасности приказывается немедленно отключить протоколы перенаправления на мостике шаттла эвакуации, используя соответствующую консоль управления.
emergency-shuttle-console-hijack-cancelled = Внимание, протоколы перенаправления отменены, эвакуация продолжается в штатном режиме!
emergency-shuttle-console-hijack-completed = Внимание, блюспейс-путь шаттла эвакуации изменён, инициирован блюспейс прыжок, всему экипажу рекомендуется покинуть шаттл эвакуации!
emergency-shuttle-ui-hijack = ПЕРЕНАПРАВЛЕНИЕ БЛЮСПЕЙС-ПУТИ
emergency-shuttle-ui-hijack-start = ИНИЦИИРОВАТЬ ПЕРЕНАПРАВЛЕНИЕ
emergency-shuttle-ui-hijack-cancel = ОТКЛЮЧИТЬ ПРОТОКОЛЫ
emergency-shuttle-ui-hijack-inactive = Протоколы перенаправления готовы
emergency-shuttle-ui-hijack-active = Изменение маршрута: { $time }
emergency-shuttle-ui-hijack-complete = Маршрут изменён
emergency-shuttle-ui-hijack-welcome = Welcome, { $name }
emergency-shuttle-ui-hijack-unknown = UNKNOWN
# Map Misc.
map-name-centcomm = Центральное Командование
map-name-terminal = Терминал прибытия
