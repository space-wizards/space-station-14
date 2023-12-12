generator-ui-title = Генератор
generator-ui-target-power-label = Целевая мощность (кВт):
generator-ui-efficiency-label = Эффективность:
generator-ui-fuel-use-label = Расход топлива:
generator-ui-fuel-left-label = Остаток топлива:
generator-insert-material = { $item } вставили в { $generator }...

generator-clogged = { $generator } неожиданно выключается!
portable-generator-verb-start = Запустить генератор
portable-generator-verb-start-msg-unreliable = Запустить генератор. Может потребовать несколько попыток.
portable-generator-verb-start-msg-reliable = Запустить генератор.
portable-generator-verb-start-msg-unanchored = Сперва это нужно прикрутить!
portable-generator-verb-stop = Остановить генератор
portable-generator-start-fail = Вы тянете за шнур, но ничего не происходит.
portable-generator-start-success = Вы тянете за шнур и генератор приходит в движение.

portable-generator-ui-title = Портативный генератор
portable-generator-ui-status-stopped = Остановлен:
portable-generator-ui-status-starting = Запускается:
portable-generator-ui-status-running = Запущен:
portable-generator-ui-start = Запустить
portable-generator-ui-stop = Остановить
portable-generator-ui-target-power-label = Целевая мощность (кВт):
portable-generator-ui-efficiency-label = Эффективность:
portable-generator-ui-fuel-use-label = Расход топлива:
portable-generator-ui-fuel-left-label = Остаток топлива:
portable-generator-ui-clogged = Посторонние вещества в топливном баке!
portable-generator-ui-eject = Извлечь
portable-generator-ui-eta = (~{ $minutes } мин)
portable-generator-ui-unanchored = Неприкручен
portable-generator-ui-current-output = Текущий выход: {$voltage}

power-switchable-generator-examine = Выход установлен на {$voltage}.
power-switchable-generator-switched = Выход установлен на {$voltage}!

power-switchable-voltage = { $voltage ->
    [HV] [color=orange]ВВ[/color]
    [MV] [color=yellow]СВ[/color]
    *[LV] [color=green]НВ[/color]
}
power-switchable-switch-voltage = Переключить на {$voltage}

fuel-generator-verb-disable-on = Генератор должен быть выключен.