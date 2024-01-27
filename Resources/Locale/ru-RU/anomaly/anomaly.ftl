anomaly-component-contact-damage = Аномалия сдирает с вас кожу!
anomaly-vessel-component-anomaly-assigned = Аномалия присвоена сосуду.
anomaly-vessel-component-not-assigned = Этому сосуду не присвоена ни одна аномалия. Попробуйте использовать на нём сканер.
anomaly-vessel-component-assigned = Этому сосуду уже присвоена аномалия.
anomaly-vessel-component-upgrade-output = генерация очков
anomaly-particles-delta = Дельта-частицы
anomaly-particles-epsilon = Эпсилон-частицы
anomaly-particles-zeta = Зета-частицы
anomaly-particles-omega = Омега-частицы
anomaly-scanner-component-scan-complete = Сканирование завершено!
anomaly-scanner-ui-title = сканер аномалий
anomaly-scanner-no-anomaly = Нет просканированной аномалии.
anomaly-scanner-severity-percentage = Текущая опасность: [color=gray]{ $percent }[/color]
anomaly-scanner-stability-low = Текущее состояние аномалии: [color=gold]Распад[/color]
anomaly-scanner-stability-medium = Текущее состояние аномалии: [color=forestgreen]Стабильное[/color]
anomaly-scanner-stability-high = Текущее состояние аномалии: [color=crimson]Рост[/color]
anomaly-scanner-point-output = Пассивная генерация очков: [color=gray]{ $point }[/color]
anomaly-scanner-particle-readout = Анализ реакции на частицы:
anomaly-scanner-particle-danger = - [color=crimson]Опасный тип:[/color] { $type }
anomaly-scanner-particle-unstable = - [color=plum]Нестабильный тип:[/color] { $type }
anomaly-scanner-particle-containment = - [color=goldenrod]Сдерживающий тип:[/color] { $type }
anomaly-scanner-pulse-timer = Время до следующего импульса: [color=gray]{ $time }[/color]
anomaly-gorilla-core-slot-name = Ядро аномалии
anomaly-gorilla-charge-none = Внутри нет [bold]ядра аномалии[/bold].
anomaly-gorilla-charge-limit =
    { $count ->
        [one] Остался
       *[other] Осталось
    } [color={ $count ->
        [3] green
        [2] yellow
        [1] orange
        [0] red
       *[other] purple
    }]{ $count } { $count ->
        [one] заряд
        [few] заряда
       *[other] зарядов
    }[/color].
anomaly-gorilla-charge-infinite = Осталось [color=gold]бесконечное количество зарядов[/color]. [italic]Пока что...[/italic]
anomaly-sync-connected = Аномалия успешно привязана
anomaly-sync-disconnected = Соединение с аномалией было потеряно!
anomaly-sync-no-anomaly = Отсутствует аномалия в пределах диапазона.
anomaly-sync-examine-connected = Он [color=darkgreen]присоединён[/color] к аномалии.
anomaly-sync-examine-not-connected = Он [color=darkred]не присоединён[/color] к аномалии.
anomaly-sync-connect-verb-text = Присоединить аномалию
anomaly-sync-connect-verb-message = Присоединить близлежащую аномалию к { $machine }.
anomaly-generator-ui-title = генератор аномалий
anomaly-generator-fuel-display = Топливо:
anomaly-generator-cooldown = Перезарядка: [color=gray]{ $time }[/color]
anomaly-generator-no-cooldown = Перезарядка: [color=gray]Завершена[/color]
anomaly-generator-yes-fire = Статус: [color=forestgreen]Готов[/color]
anomaly-generator-no-fire = Статус: [color=crimson]Не готов[/color]
anomaly-generator-generate = Создать аномалию
anomaly-generator-charges =
    { $charges ->
        [one] { $charges } заряд
        [few] { $charges } заряда
       *[other] { $charges } зарядов
    }
anomaly-generator-announcement = Аномалия была создана!
anomaly-command-pulse = Вызывает импульс аномалии
anomaly-command-supercritical = Целевая аномалия переходит в суперкритическое состояние
# Flavor text on the footer
anomaly-generator-flavor-left = Аномалия может возникнуть внутри оператора.
anomaly-generator-flavor-right = v1.1
