detailed-examine-verb-text = Подробный осмотр
detailed-examine-verb-disabled = Вам нужно подойти ближе, чтобы подробно осмотреть персонажа.
detailed-examine-window-title = Подробный осмотр

detailed-examine-age = Возраст
detailed-examine-height = Рост
detailed-examine-weight = Вес
detailed-examine-weight-value = { $weight } фунт.
detailed-examine-build = Телосложение
detailed-examine-skin-tone = Цвет кожи
detailed-examine-hair-color = Цвет волос
detailed-examine-eye-color = Цвет глаз
detailed-examine-allegiance = Принадлежность
detailed-examine-origin = Происхождение
detailed-examine-full-description = Полное описание персонажа
detailed-examine-medical-record = Медицинская карта
detailed-examine-criminal-record = Криминальное досье
detailed-examine-general-record = Общее досье

detailed-examine-age-tooltip = Возраст персонажа.
detailed-examine-height-tooltip = Рост персонажа в футах и дюймах.
detailed-examine-weight-tooltip = Вес персонажа в фунтах.
detailed-examine-build-tooltip = Телосложение персонажа.
detailed-examine-skin-tone-tooltip = Ближайшее именованное обозначение цвета кожи персонажа.
detailed-examine-hair-color-tooltip = Ближайшее именованное обозначение цвета волос персонажа.
detailed-examine-eye-color-tooltip = Ближайшее именованное обозначение цвета глаз персонажа.
detailed-examine-allegiance-tooltip = Заявленная принадлежность персонажа.
detailed-examine-origin-tooltip = Происхождение персонажа.
detailed-examine-full-description-tooltip = Полное описание внешности и манеры поведения персонажа.
detailed-examine-medical-record-tooltip = Медицинская карта персонажа. Особенности, связанные с ограничениями здоровья, указываются автоматически.
detailed-examine-criminal-record-tooltip = Криминальное досье персонажа.
detailed-examine-general-record-tooltip = Общее досье персонажа.

medical-record-disability-line = Имеет особенность: { $trait }.
medical-record-allergy-line = Аллергия на { $reagent }.

build-type-thin = Худощавое
build-type-lean = Подтянутое
build-type-average = Среднее
build-type-athletic = Атлетическое
build-type-muscular = Мускулистое
build-type-broad = Крупное
build-type-stocky = Коренастое
build-type-heavyset = Полное

faction-roster-console-window-title = Список личного состава
faction-roster-console-refresh-button = Обновить
faction-roster-console-count = { $count } { $count ->
    [one] запись
    [few] записи
    [many] записей
   *[other] записей
}
faction-roster-console-colonist-job = Колонист
faction-roster-console-count-tooltip = Количество личного состава, числящегося в базе данных.
faction-roster-console-refresh-tooltip = Повторно запросить список, чтобы учесть прибывший или выбывший личный состав.
faction-roster-console-entry-tooltip = Нажмите, чтобы открыть полное досье { $name }.

faction-roster-dossier-print = ПЕЧАТЬ ЛИЧНОГО ДЕЛА

faction-roster-print-name = личное дело ({$name})

faction-roster-print-content = [head=2]ЛИЧНОЕ ДЕЛО[/head]
    {"["}bold]{$name}[/bold], {$job}

    Возраст: {$age}
    Рост: {$height}
    Вес: {$weight}
    Телосложение: {$build}
    Цвет кожи: {$skin}
    Волосы: {$hair}
    Глаза: {$eyes}
    Лояльность: {$allegiance}
    Происхождение: {$origin}
    Религия: {$religion}

    {"["}bold]Описание[/bold]
    {$description}

    {"["}bold]Медицинская карта[/bold]
    {$medical}

    {"["}bold]Уголовное досье[/bold]
    {$criminal}

    {"["}bold]Общие сведения[/bold]
    {$general}
