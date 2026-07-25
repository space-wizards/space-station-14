game-preset-system-toggle = Включить автоматические голосования
game-preset-settings-button = Настройки
game-preset-save-changes = Сохранить
game-preset-active-tab = Активные пресеты
game-preset-custom-presets = Пресеты сервера
game-preset-edit-button = Редактировать
game-preset-add-button = Добавить
game-preset-save-button = Сохранить
game-preset-create-window-title = Создать пресет
game-preset-name-placeholder = Название пресета
game-preset-type-rdm = РДМ
game-preset-settings-window-title = Настройки пресетов
game-preset-settings-enable-max-rdm-row = Ограничение РДМ подряд
game-preset-menu-button = Меню пресетов
game-preset-window-title = Управление пресетами
game-preset-vote-title = Следующий режим игры
game-preset-vote-no-presets = Ошибка запуска голосования за пресет: не настроены активные пресеты.
game-preset-settings-vote-duration = Длительность голосования (сек)
game-preset-unsaved-changes-title = Несохранённые изменения
game-preset-unsaved-changes-message = У вас есть несохранённые изменения в очереди пресетов.
game-preset-unsaved-save = Сохранить и выйти
game-preset-unsaved-discard = Выйти без сохранения
game-preset-create-window-title-democracy = Создать комбинированный пресет
game-preset-refresh-button = Обновить
game-preset-add-democracy-button =
    Добавить
    комбинированный
    пресет
game-preset-initiate-vote-button = Инициировать голосование
game-preset-skip-button = Пропустить
game-preset-rdm-limit-exceeded = Внимание: превышен лимит РДМ подряд, но нет спокойного пресета для голосования. Голосование продолжается согласно расписанию.
game-preset-type-secret = Секрет
game-preset-secret-win = Секретный режим победил в голосовании.
game-preset-settings-disable-ooc = Отключать OOC во время голосования
game-preset-settings-check-player-limit = Проверка лимита игроков
game-preset-settings-prevent-repeat = Запретить повтор режима
game-preset-rdm-skipped = Пресет "{ $preset }" пропущен из-за лимита РДМ подряд.
game-preset-democracy-all-rdm-skipped = Комбинированный пресет "{ $preset }" пропущен: все подпресеты являются РДМ.
game-preset-democracy-rdm-removed = Подпресет "{ $subpreset }" удалён из комбинированного пресета "{ $parent }" из-за лимита РДМ.
game-preset-democracy-nested-all-rdm-removed = Вложенный комбинированный пресет "{ $subpreset }" полностью удалён из "{ $parent }", так как все его подпресеты РДМ.
game-preset-secret-win-admin = Был выбран режим: {$mode}
game-preset-mode-set = Был установлен режим: {$mode}
game-preset-mode-tie = Ничья в голосовании за игровой режим! Выбирается... { $picked }
game-preset-player-limit-removed-single = Режим { $mode } удалён из голосования: недостаточно игроков.
game-preset-player-limit-removed-multiple = Режимы { $modes } удалены из голосования: недостаточно игроков.
game-preset-skipped-no-modes = Пресет "{$preset}" пропущен: нет доступных режимов.
game-preset-player-limit-preset-removed-single = Пресет { $preset } удалён из голосования: недостаточно игроков.
game-preset-player-limit-preset-removed-multiple = Пресеты { $presets } удалены из голосования: недостаточно игроков.
game-preset-settings-whitelist = Вайтлист
game-preset-whitelist-window-title = Вайтлист режимов
game-preset-whitelist-description = Отмеченные режимы будут игнорировать запрет повтора
game-preset-all-presets-skipped = Внимание: нет подходящего пресета для голосования. Ограничения игнорируются и голосование продолжается согласно расписанию.
game-preset-initiate-vote-tooltip =
    Мгновенно запустить голосование по текущему пресету.
    При ручном запуске игнорируется лимит РДМ подряд
    и не изменяется текущий стрик, в голосование попадают
    все режимы (включая РДМ) из пресета.

game-preset-skip-tooltip =
    Пропускает текущий пресет и переводит выделение на следующий.

game-preset-rdm-counter-tooltip =
    Максимальное количество РДМ пресетов подряд.
    При достижении этого лимита следующие РДМ пресеты будут
    автоматически пропускаться до ближайшего спокойного.
    Спокойные пресеты в расписании сбрасывают счётчик.
    Счётчик текущего стрика отображается в главном окне.

game-preset-type-rdm-tooltip =
    Пометить пресет как РДМ. Когда такой пресет побеждает в голосовании
    или запускается по расписанию, увеличивается счётчик РДМ подряд.
    Если включено ограничение, при достижении лимита
    следующие РДМ пресеты будут пропущены, пока не встретится
    спокойный или комбинированный пресет с не РДМ составляющими.

game-preset-type-secret-tooltip =
    Скрыть результаты голосования от игроков.
    Победивший режим не объявляется в чате и
    будет заменён на его секретный аналог.
    Администраторы увидят реальный выбранный режим
    в оповещении только для администрации.

game-preset-add-button-tooltip =
    Создать новый пресет из игровых режимов.
    Выберите один или несколько режимов, задайте название
    и при необходимости пометьте как РДМ или Секрет.
    После создания пресет появится в списке доступных
    для добавления в очередь.

game-preset-add-democracy-button-tooltip =
    Создать комбинированный пресет. В него добавляются
    уже существующие пресеты (в том числе другие комбинированные).
    При голосовании сначала выбирается пресет,
    а затем запускается голосование по его режимам.

game-preset-settings-check-player-limit-tooltip =
    Если включено, режимы, для которых не хватает игроков на сервере,
    не будут добавлены в голосование.

game-preset-settings-prevent-repeat-tooltip =
    Если включено, режим, выигравший предыдущее голосование,
    не будет участвовать в следующем.

game-preset-settings-whitelist-tooltip = Режимы из этого списка будут игнорировать эту настройку.