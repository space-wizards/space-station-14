# UI

## Window

air-alarm-ui-title = Воздушная сигнализация
air-alarm-ui-access-denied = Недостаточный уровень доступа!
air-alarm-ui-window-pressure-label = Давление
air-alarm-ui-window-temperature-label = Температура
air-alarm-ui-window-alarm-state-label = Статус
air-alarm-ui-window-address-label = Адрес
air-alarm-ui-window-device-count-label = Всего устройств
air-alarm-ui-window-resync-devices-label = Ресинхр
air-alarm-ui-window-mode-label = Режим
air-alarm-ui-window-mode-select-locked-label = [bold][color=red] Ошибка выбора режима! [/color][/bold]
air-alarm-ui-window-auto-mode-label = Авто-режим
-air-alarm-state-name =
    { $state ->
        [normal] Нормально
        [warning] Предупреждение
        [danger] Опасно
        [emagged] Взломано
       *[invalid] Невалидно
    }
air-alarm-ui-window-listing-title = {$address} : {-air-alarm-state-name(state:$state)}
air-alarm-ui-window-pressure = { $pressure } кПа
air-alarm-ui-window-pressure-indicator = Давление: [color={ $color }]{ $pressure } кПа[/color]
air-alarm-ui-window-temperature = { $tempC } °C ({ $temperature } К)
air-alarm-ui-window-temperature-indicator = Температура: [color={ $color }]{ $tempC } °C ({ $temperature } К)[/color]
air-alarm-ui-window-alarm-state = [color={ $color }]{-air-alarm-state-name(state:$state)}[/color]
air-alarm-ui-window-alarm-state-indicator = Статус: [color={ $color }]{-air-alarm-state-name(state:$state)}[/color]
air-alarm-ui-window-tab-vents = Вентиляции
air-alarm-ui-window-tab-scrubbers = Скрубберы
air-alarm-ui-window-tab-sensors = Сенсоры
air-alarm-ui-gases = { $gas }: { $amount } моль ({ $percentage }%)
air-alarm-ui-gases-indicator = { $gas }: [color={ $color }]{ $amount } моль ({ $percentage }%)[/color]
air-alarm-ui-mode-filtering = Фильтрация
air-alarm-ui-mode-wide-filtering = Фильтрация (широкая)
air-alarm-ui-mode-fill = Заполнение
air-alarm-ui-mode-panic = Паника
air-alarm-ui-mode-none = Нет
air-alarm-ui-pump-direction-siphoning = Откачка
air-alarm-ui-pump-direction-scrubbing = Фильтрация
air-alarm-ui-pump-direction-releasing = Выпуск
air-alarm-ui-pressure-bound-nobound = Без границы
air-alarm-ui-pressure-bound-internalbound = Внутренний порог
air-alarm-ui-pressure-bound-externalbound = Внешний порог
air-alarm-ui-pressure-bound-both = Оба
air-alarm-ui-widget-gas-filters = Фильтр газов

## Widgets

### General

air-alarm-ui-widget-enable = Включено
air-alarm-ui-widget-copy = Копировать настройки на похожие устройства
air-alarm-ui-widget-copy-tooltip = Копирует настройки данного устройства на все устройства данной вкладки воздушной сигнализации.
air-alarm-ui-widget-ignore = Игнорировать
air-alarm-ui-atmos-net-device-label = Адрес: { $address }

### Vent pumps

air-alarm-ui-vent-pump-label = Направление вентиляции
air-alarm-ui-vent-pressure-label = Ограничение давления
air-alarm-ui-vent-external-bound-label = Внешний порог
air-alarm-ui-vent-internal-bound-label = Внутренний порог

### Scrubbers

air-alarm-ui-scrubber-pump-direction-label = Направление
air-alarm-ui-scrubber-volume-rate-label = Объём (Л)
air-alarm-ui-scrubber-wide-net-label = ШирокаяСеть
air-alarm-ui-scrubber-select-all-gases-label = Включить все
air-alarm-ui-scrubber-deselect-all-gases-label = Отключить все

### Thresholds

air-alarm-ui-sensor-gases = Газы
air-alarm-ui-sensor-thresholds = Границы
air-alarm-ui-thresholds-pressure-title = Границы (кПа)
air-alarm-ui-thresholds-temperature-title = Границы (К)
air-alarm-ui-thresholds-gas-title = Границы (%)
air-alarm-ui-thresholds-upper-bound = Верхний аварийный порог
air-alarm-ui-thresholds-lower-bound = Нижний аварийный порог
air-alarm-ui-thresholds-upper-warning-bound = Верхний тревожный порог
air-alarm-ui-thresholds-lower-warning-bound = Нижний тревожный порог
air-alarm-ui-thresholds-copy = Скопировать значение границы на все устройства
air-alarm-ui-thresholds-copy-tooltip = Скопировать значение границы сенсора этого устройства на все устройства на этой вкладке воздушной сигнализации.
