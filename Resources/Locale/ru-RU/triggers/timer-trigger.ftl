timer-trigger-verb-set =
    { $time ->
        [one] { $time } секунда
        [few] { $time } секунды
       *[other] { $time } секунд
    }
timer-trigger-verb-set-current =
    { $time ->
        [one] { $time } секунда
        [few] { $time } секунды
       *[other] { $time } секунд
    } (сейчас)
timer-trigger-verb-cycle = Переключить задержку
timer-trigger-examine =
    Таймер установлен на { $time ->
        [one] { $time } секунду
        [few] { $time } секунды
       *[other] { $time } секунд
    }.
timer-trigger-popup-set =
    Таймер установлен на { $time ->
        [one] { $time } секунду
        [few] { $time } секунды
       *[other] { $time } секунд
    }.
timer-trigger-activated = Вы активировали { $device }.
