-create-3rd-person =
    { $chance ->
        [1] Создаёт
       *[other] создают
    }
-cause-3rd-person =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    }
-satiate-3rd-person =
    { $chance ->
        [1] Насыщает
       *[other] насыщают
    }
reagent-effect-guidebook-create-entity-reaction-effect =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } { $amount ->
        [1] { $entname }
       *[other] { $amount } { $entname }
    }
reagent-effect-guidebook-explosion-reaction-effect =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } взрыв
reagent-effect-guidebook-emp-reaction-effect =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } электромагнитный импульс
reagent-effect-guidebook-flash-reaction-effect =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } ослепительную вспышку
reagent-effect-guidebook-foam-area-reaction-effect =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } большое количество пены
reagent-effect-guidebook-smoke-area-reaction-effect =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } большое количество дыма
reagent-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Утоляет
       *[other] утоляют
    } { $relative ->
        [1] жажду средне
       *[other] жажду на { NATURALFIXED($relative, 3) }x от обычного
    }
reagent-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Насыщает
       *[other] насыщают
    } { $relative ->
        [1] голод средне
       *[other] голод на { NATURALFIXED($relative, 3) }x от обычного
    }
reagent-effect-guidebook-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Излечивает
                [deals] Наносит
               *[both] Изменяет здоровье на
            }
       *[other]
            { $healsordeals ->
                [heals] излечивать
                [deals] наносить
               *[both] изменяют здоровье на
            }
    } { $changes }
reagent-effect-guidebook-even-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Evenly heals
                [deals] Evenly deals
               *[both] Evenly modifies health by
            }
       *[other]
            { $healsordeals ->
                [heals] evenly heal
                [deals] evenly deal
               *[both] evenly modify health by
            }
    } { $changes }
reagent-effect-guidebook-status-effect =
    { $type ->
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) }, эффект накапливается
       *[set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) }, эффект не накапливается
        [remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } { NATURALFIXED($time, 3) } от { LOC($key) }
    }
reagent-effect-guidebook-activate-artifact =
    { $chance ->
        [1] Пытается
       *[other] пытаются
    } активировать артефакт
reagent-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Устанавливает
       *[other] устанавливают
    } температуру раствора точно { NATURALFIXED($temperature, 2) }k
reagent-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } тепло из раствора, пока температура не достигнет { $deltasign ->
        [1] не более { NATURALFIXED($maxtemp, 2) }k
       *[-1] не менее { NATURALFIXED($mintemp, 2) }k
    }
reagent-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляют
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { NATURALFIXED($amount, 2) } ед. от { $reagent } { $deltasign ->
        [1] к
       *[-1] из
    } раствора
reagent-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { NATURALFIXED($amount, 2) }ед реагентов в группе { $group } { $deltasign ->
        [1] к
       *[-1] из
    } раствора
reagent-effect-guidebook-adjust-temperature =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляют
               *[-1] Удаляют
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { POWERJOULES($amount) } тепла { $deltasign ->
        [1] к телу
       *[-1] из тела
    }, в котором он метабилизируется
reagent-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } болезнь { $disease }
reagent-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } болезнь { $diseases }
reagent-effect-guidebook-jittering =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } тряску
reagent-effect-guidebook-chem-clean-bloodstream =
    { $chance ->
        [1] Очищает
       *[other] очищают
    } кровеносную систему от других веществ
reagent-effect-guidebook-cure-disease =
    { $chance ->
        [1] Излечивает
       *[other] излечивают
    } болезнь
reagent-effect-guidebook-cure-eye-damage =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Наносит
               *[-1] Излечивает
            }
       *[other]
            { $deltasign ->
                [1] наносят
               *[-1] излечивают
            }
    } повреждения глаз
reagent-effect-guidebook-chem-vomit =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } рвоту
reagent-effect-guidebook-create-gas =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } { $moles } { $moles ->
        [1] моль
       *[other] моль
    } газа { $gas }
reagent-effect-guidebook-drunk =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } опьянение
reagent-effect-guidebook-emote =
    { $chance ->
        [1] Will force
       *[other] force
    } the metabolizer to [bold][color=white]{ $emote }[/color][/bold]
reagent-effect-guidebook-electrocute =
    { $chance ->
        [1] Бьёт током
       *[other] бьют током
    } употребившего в течении { NATURALFIXED($time, 3) }
reagent-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Гасит
       *[other] гасят
    } огонь
reagent-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } воспламеняемость
reagent-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
       *[other] поджигают
    } употребившего
reagent-effect-guidebook-make-sentient =
    { $chance ->
        [1] Делает
       *[other] делают
    } употребившего разумным
reagent-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Превращает
       *[other] превращают
    } употребившего в { $entityname }
reagent-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Усиливает
               *[-1] Ослабляет
            }
       *[other]
            { $deltasign ->
                [1] усиливают
               *[-1] ослабляют
            }
    } кровотечение
reagent-effect-guidebook-modify-blood-level =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Повышает
               *[-1] Понижает
            }
       *[other]
            { $deltasign ->
                [1] повышают
               *[-1] понижают
            }
    } уровень крови в организме
reagent-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
       *[other] парализуют
    } употребившего минимум на { NATURALFIXED($time, 3) }
reagent-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Делает
       *[other] делают
    } скорость передвижения { NATURALFIXED($walkspeed, 3) }x от стандартной минимум на { NATURALFIXED($time, 3) }
reagent-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Предотвращает
       *[other] предотвращают
    } приступы нарколепсии
reagent-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает
       *[other] смывают
    } кремовый пирог с лица
reagent-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Лечит
       *[other] лечат
    } зомби-вирус
reagent-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Заражает
       *[other] заражают
    } человека зомби-вирусом
reagent-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Регенерирует
       *[other] регенерируют
    } { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    } гниения
reagent-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Лечит
       *[other] лечат
    } зомби-вирус и обеспечивает иммунитет к нему в будущем
reagent-effect-guidebook-area-reaction =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } дымовую или пенную реакцию на { NATURALFIXED($duration, 3) } { $duration ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }
reagent-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Helps
       *[other] help
    } unlock an alien artifact.
reagent-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Заставляет
       *[other] заставляют
    } химикаты, применённые к объекту, добавиться во внутренний контейнер для растворов этого объекта
reagent-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } { $attribute } за [color={ $colorName }]{ $amount }[/color]
reagent-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает
       *[other] омолаживают
    } растение, в зависимости от возраста растения и времени его роста
reagent-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливают
    } жизнеспособность растения, ставшего нежизнеспособным в результате мутации
reagent-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } продолжительность жизни растения и/или его базовое здоровье с шансом 10% на единицу
reagent-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } потенцию растения путём { $increase } до максимума в { $limit }. Приводит к тому, что растение теряет свои семена, когда потенция достигает { $seedlesstreshold }. Попытка повысить потенцию свыше { $limit } может вызвать снижение урожайности с вероятностью 10%
reagent-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливают
    } семена растения
reagent-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Убирает
       *[other] убирают
    } семена из растения
