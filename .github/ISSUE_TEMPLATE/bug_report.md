name: "Внести задачу/предложение"
description: "Если что-то нужно поменять или починить."
labels: ["triage"]
body:
  - type: textarea
    id: description
    validations:
      required: true
    attributes:
      label: "Описание"
      description: "Опишите проблему/предложение как можно подробнее"

  - type: textarea
    id: reproduction
    attributes:
      label: "Шаги воспроизведения (только для ошибок)"
      description: "Если приемлемо, опишите шаги для воспроизведения проблемы"
      placeholder: |
        1. Открыть интерфейс консоли
        2. Нажать кнопку "Старт"
        3. Получить ошибку...

  - type: textarea
    id: screenshots
    attributes:
      label: "Скриншоты (референсы/скрины ошибки)"
      description: |
        Если приемлемо, добавьте скриншоты, чтобы помочь объяснить вашу проблему
        **Подсказка**: Вы можете прикрепить изображения щелкнув по области ниже, чтобы выделить её, а затем перетащив в нее файлы
