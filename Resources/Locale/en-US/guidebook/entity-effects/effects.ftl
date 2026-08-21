-create-3rd-person =
    { $chance ->
        [1] Создаёт
        *[other] создать
    }

-cause-3rd-person =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    }

-satiate-3rd-person =
    { $chance ->
        [1] Насыщает
        *[other] насытить
    }

entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Создаёт
        *[other] создать
    } { $amount ->
        [1] { INDEFINITE($entname) }
        *[other] { $amount } { MAKEPLURAL($entname) }
    }

entity-effect-guidebook-destroy =
    { $chance ->
        [1] Уничтожает
        *[other] уничтожить
    } объект

entity-effect-guidebook-break =
    { $chance ->
        [1] Ломает
        *[other] сломать
    } объект

entity-effect-guidebook-explosion =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    } взрыв

entity-effect-guidebook-emp =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    } электромагнитный импульс

entity-effect-guidebook-flash =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    } ослепительную вспышку

entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    } большое количество пены

entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    } большое количество дыма

entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Утоляет
        *[other] утолить
    } { $relative ->
        [1] жажду
        *[other] жажду с { NATURALFIXED($relative, 3) }х эффективностью
    }

entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Утоляет
        *[other] утолить
    } { $relative ->
        [1] голод
        *[other] голод с { NATURALFIXED($relative, 3) }х эффективностью
    }

entity-effect-guidebook-health-change =
    { $chance ->
        [1] { $healsordeals ->
                [heals] Лечит
                [deals] Наносит
                *[both] Изменяет здоровье на
            }
        *[other] { $healsordeals ->
                [heals] вылечить
                [deals] нанести
                *[both] изменить здоровье на
            }
    } { $changes }

entity-effect-guidebook-even-health-change =
    { $chance ->
        [1] { $healsordeals ->
                [heals] Равномерно лечит
                [deals] Равномерно наносит
                *[both] Равномерно изменяет здоровье на
            }
        *[other] { $healsordeals ->
                [heals] равномерно вылечить
                [deals] равномерно нанести
                *[both] равномерно изменить здоровье на
            }
    } { $changes }

entity-effect-guidebook-status-effect-old =
    { $type ->
        [update]{ $chance ->
                [1] Вызывает
                *[other] вызвать
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } без накопления эффекта
        [add]   { $chance ->
                [1] Вызывает
                *[other] вызвать
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } с накоплением эффекта
        [set]  { $chance ->
                [1] Вызывает
                *[other] вызвать
            } { LOC($key) } на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } без накопления эффекта
        *[remove]{ $chance ->
                [1] Удаляет
                *[other] удалить
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } { LOC($key) }
    }

entity-effect-guidebook-status-effect =
    { $type ->
        [update]{ $chance ->
                [1] Вызывает
                *[other] вызывают
            } { $key } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            }, эффект не накапливается
        [add] { $chance ->
                [1] Вызывает
                *[other] вызывают
            } { $key } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            }, эффект накапливается
        [set] { $chance ->
                [1] Вызывает
                *[other] вызывают
            } { $key } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            }, эффект не накапливается
        *[remove] { $chance ->
                [1] Удаляет
                *[other] удаляют
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } от { $key }
    } { $delay ->
        [0] немедленно
        *[other] после { NATURALFIXED($delay, 3) } { $delay ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } задержки
    }

entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]{ $chance ->
                [1] Вызывает
                *[other] вызывает
            } постоянный { $key }
        [add]   { $chance ->
                [1] Вызывает
                *[other] вызывают
            } постоянный{ $key }
        [set]  { $chance ->
                [1] Вызывает
                *[other] вызывают
            } постоянный{ $key }
        *[remove]{ $chance ->
                [1] Убирает
                *[other] убирают
            } { $key }
    } { $delay ->
        [0] мгновенно
        *[other] после { NATURALFIXED($delay, 3) } { $delay ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } задержки
    }

entity-effect-guidebook-knockdown =
    { $type ->
        [update]{ $chance ->
                [1] Вызывает
                *[other] вызвать
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } без накопления эффекта
        [add]   { $chance ->
                [1] Вызывает
                *[other] вызвать
            } нокаут миниум { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } с накоплением эффекта
        *[set]  { $chance ->
                [1] Вызывает
                *[other] вызвать
            } нокаут миниум { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } без накопления эффекта
        [remove]{ $chance ->
                [1] Удаляет
                *[other] удалить
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } нокаута
    }

entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Sets
        *[other] set
    } the solution temperature to exactly { NATURALFIXED($temperature, 2) }k

entity-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1] { $deltasign ->
                [1] Adds
                *[-1] Removes
            }
        *[other] { $deltasign ->
                [1] add
                *[-1] remove
            }
    } heat from the solution until it reaches { $deltasign ->
        [1] at most { NATURALFIXED($maxtemp, 2) }k
        *[-1] at least { NATURALFIXED($mintemp, 2) }k
    }

entity-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Удаляет
            }
        *[other] { $deltasign ->
                [1] добавить
                *[-1] удалить
            }
    } { NATURALFIXED($amount, 2) }ед. { $reagent } { $deltasign ->
        [1] в кровь
        *[-1] из крови
    }

entity-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Удаляет
            }
        *[other] { $deltasign ->
                [1] добавляет
                *[-1] удаляет
            }
    } { NATURALFIXED($amount, 2) }ед. реагентов из группы { $group } { $deltasign ->
        [1] в кровь
        *[-1] из крови
    }

entity-effect-guidebook-adjust-temperature =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Удаляет
            }
        *[other] { $deltasign ->
                [1] добавляет
                *[-1] удаляет
            }
    } { POWERJOULES($amount) } тепла { $deltasign ->
        [1] в организм
        *[-1] из организма
    }

entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } болезнь { $disease }

entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } болезньи { $diseases }

entity-effect-guidebook-jittering =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } дрожание

entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Очищает
        *[other] очищает
    } кровоток от других реагентов

entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Лечит
        *[other] лечит
    } болезни

entity-effect-guidebook-eye-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Повреждает
                *[-1] Лечит
            }
        *[other] { $deltasign ->
                [1] повреждает
                *[-1] лечит
            }
    } глаза

entity-effect-guidebook-vomit =
    { $chance ->
        [1] Вызывает
        *[other] вызывать
    } рвоту

entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Создаёт
        *[other] создаёт
    } { $moles } { $moles ->
        [1] моль
        *[other] моль
    } { $gas }

entity-effect-guidebook-drunk =
    { $chance ->
        [1] Вызывает
        *[other] вызвать
    } опьянение

entity-effect-guidebook-electrocute =
    { $chance ->
        [1] Поражает электрическим током
        *[other] поразить электрическим током
    } на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
        *[other] секунд
    }

entity-effect-guidebook-emote =
    { $chance ->
        [1] Заставляет
        *[other] заставить
    } [bold][color=white]{ $emote }[/color][/bold]

entity-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Тушит
        *[other] потушить
    } огонь

entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Увеличивает
        *[other] увеличить
    } воспламеняемость

entity-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
        *[other] поджечь
    } the metabolizer

entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] Делает
        *[other] сделать
    } разумным

entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Превращает
        *[other] превратить
    } в { $entityname }

entity-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1] { $deltasign ->
                [1] Вызывает
                *[-1] Уменьшает
            }
        *[other] { $deltasign ->
                [1] вызвать
                *[-1] уменьшить
            }
    } кровотечение

entity-effect-guidebook-modify-blood-level =
    { $chance ->
        [1] { $deltasign ->
                [1] Увеличивает
                *[-1] Уменьшает
            }
        *[other] { $deltasign ->
                [1] увеличить
                *[-1] уменьшить
            }
    } уровень крови

entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
        *[other] парализовать
    } минимум на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
        *[other] секунд
    }

entity-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Изменяет
        *[other] изменить
    } скорость бега на { NATURALFIXED($sprintspeed, 3) }х минимум на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
        *[other] секунд
    }

entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Временно предотвращает
        *[other] временно предотвратить
    } нарколепсию

entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает
        *[other] смыть
    } кремовый пирог с лица

entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Лечит
        *[other] вылечить
    } развивающийся зомби-вирус

entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Заражает
        *[other] заразить
    } зомби-вирусом

entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Лечит
        *[other] вылечить
    } зомби-вирус и обеспечивает иммунитет к нему в будущем

entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Регенерирует
        *[other] регенерировать
    } { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
        *[other] секунд
    } гниения

entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Вызывает
        *[other] вызвать
    } реакцию дыма или пены на { NATURALFIXED($duration, 3) } { MANY("секунд", $duration) }

entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Вызывает
        *[other] вызвать
    } добавление { $reagent } в текущую ёмкость

entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Помогает
        *[other] помогают
    } разблокировать инопланетный артефакт.

entity-effect-guidebook-artifact-durability-restore =
    Восстанавливает { $restored } прочности активных узлов артефакта.

entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Изменяет
        *[other] изменить
    } { $attribute } на { $positive ->
        [false] [color=red]{ $amount }[/color]
        *[true] [color=green]{ $amount }[/color]
    }

entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает
        *[other] омолодить
    } растение, в зависимости от возраста растения и времени его роста

entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Восстанавливает
        *[other] восстанавливают
    } жизнеспособность растения, ставшего нежизнеспособным в результате мутации

entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Повышает
        *[other] повышают
    } продолжительность жизни растения и/или его базовое здоровье с шансом 10% на единицу

entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Повышает
        *[other] повышают
    } потенцию растения путём { $increase } до максимума в { $limit }. Приводит к тому, что растение теряет свои семена, когда потенция достигает { $seedlesstreshold }. Попытка повысить потенцию свыше { $limit } может вызвать снижение урожайности с вероятностью 10%

entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает
        *[other] восстанавливают
    } семена растения

entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Убирает
        *[other] убирают
    } семена из растения

entity-effect-guidebook-plant-mutate-exude-gasses =
    { $chance ->
        [1] Мутирует
        *[other] мутировать
    } выделение растением газов от {$minValue} до {$maxValue} молей

entity-effect-guidebook-plant-mutate-consume-gasses =
    { $chance ->
        [1] Мутирует
        *[other] мутировать
    } потребление растением газов от {$minValue} до {$maxValue} молей

entity-effect-guidebook-plant-mutate-chemicals =
    { $chance ->
        [1] Мутирует
        *[other] мутируют
    } растение, чтобы то производило { $name }

entity-effect-guidebook-add-reagent-to-bloodstream =
    { $chance ->
        [1] Вводит
        *[other] вводят
    } { $quantity } { $reagent } напрямую в кровоток

entity-effect-disarm =
    { $chance ->
        [1] Обезоруживает
        *[other] обезоружить
    } цель
