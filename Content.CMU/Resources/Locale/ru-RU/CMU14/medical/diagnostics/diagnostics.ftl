cmu-medical-scanner-body-map-header        = Карта тела
cmu-medical-scanner-pulse-label            = Пульс:
cmu-medical-scanner-body-parts-header      = Части тела
cmu-medical-scanner-organs-header          = Органы
cmu-medical-scanner-fractures-header       = Переломы
cmu-medical-scanner-bleeds-header          = Внутреннее кровотечение
cmu-medical-scanner-pulse-stopped          = [color=red][bold]Пульс отсутствует — сердце остановилось[/bold][/color]
cmu-medical-scanner-pulse-bpm              = { $bpm } уд/мин
cmu-medical-scanner-part-line              = { $part }: { $current }/{ $max } ОЗ
cmu-medical-scanner-part-suffix-splinted   = (наложена шина)
cmu-medical-scanner-part-suffix-cast       = (в гипсе)
cmu-medical-scanner-part-suffix-wounds     = ({ $count } { $count ->
    [one] рана
    [few] раны
    [many] ран
   *[other] раны
})
cmu-medical-scanner-organ-line             = { $organ }: { $stage } ({ $current }/{ $max })
cmu-medical-scanner-organ-removed          = { $organ }: [color=red]УДАЛЁН[/color]
cmu-medical-scanner-fracture-line-exact    = { $part }: перелом — { $severity }
cmu-medical-scanner-fracture-line-vague    = { $part }: обнаружен перелом
cmu-medical-scanner-fracture-suppressed    = (подавлен)
cmu-medical-scanner-bleed-exact            = { $part }: кровопотеря { $rate}/сек
cmu-medical-scanner-bleed-vague            = Обнаружено внутреннее кровотечение (местоположение неизвестно)

cmu-medical-stethoscope-pulse              = Частота сердечных сокращений: { $bpm } уд/мин.
cmu-medical-stethoscope-pulse-qualitative  = Пульс: { $description }.
cmu-medical-stethoscope-no-pulse           = Сердцебиение не обнаружено.
cmu-medical-stethoscope-no-heart           = В грудной клетке пациента отсутствует сердце.
cmu-medical-stethoscope-lungs-precise      = Лёгкие: { $stage }.
cmu-medical-stethoscope-lungs-qualitative  = Дыхание в лёгких: { $description }.
cmu-medical-stethoscope-no-lungs           = В грудной клетке пациента отсутствуют лёгкие.

cmu-medical-scanner-section-head           = Голова
cmu-medical-scanner-section-torso          = Туловище
cmu-medical-scanner-section-arms           = Руки
cmu-medical-scanner-section-legs           = Ноги
cmu-medical-scanner-section-organs         = Органы
cmu-medical-scanner-hp                     = ОЗ
cmu-medical-scanner-bone                   = Кость
cmu-medical-scanner-fracture               = Перелом: { $severity }
cmu-medical-scanner-fracture-vague         = Перелом: обнаружен
cmu-medical-scanner-bleed-internal         = Внутреннее кровотечение
cmu-medical-scanner-pain-unknown           = Боль: ?
cmu-medical-scanner-pain-none              = Боль: отсутствует
cmu-medical-scanner-pain-mild              = Боль: слабая
cmu-medical-scanner-pain-moderate          = Боль: умеренная
cmu-medical-scanner-pain-severe            = Боль: сильная
cmu-medical-scanner-pain-shock             = Боль: шок
cmu-medical-scanner-pain-risk-unknown      = ?
cmu-medical-scanner-pain-risk-low          = Низкий
cmu-medical-scanner-pain-risk-elevated     = Повышенный
cmu-medical-scanner-pain-risk-high         = Высокий
cmu-medical-scanner-pain-risk-imminent     = Неминуемый
cmu-medical-scanner-pain-risk-active       = Активный
cmu-medical-scanner-pain-risk-suppressed-suffix =  (подавл.)

# Редизайн V2-ε в формате медицинской сводки — тёмные карточки + баннер состояния + схема тела
cmu-medical-scanner-card-body              = Тело
cmu-medical-scanner-card-organs            = Органы
cmu-medical-scanner-card-reagents          = Реагенты в крови
cmu-medical-scanner-card-recommended       = Рекомендации
cmu-medical-scanner-card-patient           = Пациент
cmu-medical-scanner-card-damage            = Профиль повреждений
cmu-medical-scanner-loading                = Получение данных сканирования
cmu-medical-scanner-loading-subtext        = обработка состояния сервера

cmu-medical-scanner-stat-health            = ЗДОРОВЬЕ
cmu-medical-scanner-stat-pulse             = ПУЛЬС, УД/МИН
cmu-medical-scanner-stat-blood             = КРОВЬ
cmu-medical-scanner-stat-temp              = ТЕМП. °C
cmu-medical-scanner-stat-shock-risk        = РИСК ШОКА
cmu-medical-scanner-stat-pulse-stopped     = 0
cmu-medical-scanner-stat-deceased-short    = МЁРТВ

cmu-medical-scanner-status-stable          = СТАБИЛЬНО
cmu-medical-scanner-status-serious         = ТЯЖЁЛОЕ
cmu-medical-scanner-status-critical        = КРИТИЧЕСКОЕ
cmu-medical-scanner-status-deceased        = МЁРТВ

cmu-medical-scanner-severity-healthy       = Здоров
cmu-medical-scanner-severity-bruised       = Ушиблен
cmu-medical-scanner-severity-damaged       = Повреждён
cmu-medical-scanner-severity-critical      = Критическое
cmu-medical-scanner-severity-severed       = Отсечён

cmu-medical-scanner-chip-fracture-vague    = Перелом
cmu-medical-scanner-chip-suppressed-suffix =  (подавл.)
cmu-medical-scanner-chip-bleed             = ВК
cmu-medical-scanner-chip-bleeding          = Кровотечение
cmu-medical-scanner-chip-shrapnel          = осколков: { $count }
cmu-medical-scanner-chip-splint            = Шина
cmu-medical-scanner-chip-cast              = Гипс
cmu-medical-scanner-chip-tourniquet        = Жгут
cmu-medical-scanner-eschar                 = струп
cmu-medical-scanner-chip-wounds            = { $count } { $count ->
    [one] рана
    [few] раны
    [many] ран
   *[other] раны
}

# Подсказки по уровню навыка — показывают, что именно осматривающий не способен определить,
# чтобы медик понимал, что ему не хватает подготовки, а не считал пациента здоровым.
cmu-medical-scanner-skill-hint-fractures   = Недостаточно подготовки для обнаружения переломов и внутренних кровотечений (требуется Медицина-1).
cmu-medical-scanner-skill-hint-organs      = Недостаточно подготовки для оценки повреждений органов (требуется Медицина-2).
cmu-medical-scanner-synthetic-physiology   = Обнаружена синтетическая физиология

# Устаревшие ключи V2-ε Mix B (всё ещё используются тестами и резервными путями)
cmu-medical-scanner-vitals-pain            = Боль
cmu-medical-scanner-stable-summary         = Стабильно: { $list }
cmu-medical-scanner-acute-issues-header    = Острые состояния
cmu-medical-scanner-acute-severed          = Отсечено: { $part }
cmu-medical-scanner-acute-fracture         = Перелом ({ $severity }): { $part }
cmu-medical-scanner-acute-fracture-vague   = Перелом: { $part }
cmu-medical-scanner-acute-bleed            = Внутреннее кровотечение: { $part }
cmu-medical-scanner-acute-bleed-vague      = Обнаружено внутреннее кровотечение
cmu-medical-scanner-acute-organ            = { $stage }: { $organ }
cmu-medical-scanner-acute-organ-removed    = Удалён: { $organ }
cmu-medical-scanner-organ-removed-short    = Удалён

# Отображаемые названия органов — понятные названия, привязанные к идентификаторам
# прототипов CMUOrganHuman*. Отдельные ключи для каждого органа позволяют при
# переименовании в V2.5 менять только локализацию.
cmu-medical-scanner-organ-heart            = Сердце
cmu-medical-scanner-organ-lungs            = Лёгкие
cmu-medical-scanner-organ-liver            = Печень
cmu-medical-scanner-organ-brain            = Мозг
cmu-medical-scanner-organ-kidneys          = Почки
cmu-medical-scanner-organ-stomach          = Желудок
cmu-medical-scanner-organ-eyes             = Глаза

cmu-medical-stethoscope-pain-mild          = Пациент испытывает небольшой дискомфорт.
cmu-medical-stethoscope-pain-moderate      = Пациент испытывает заметную боль.
cmu-medical-stethoscope-pain-severe        = Пациент испытывает сильную боль.
cmu-medical-stethoscope-pain-shock         = Пациент находится в состоянии шока.
cmu-medical-scanner-chip-external-bleed = Кровотечение: {$tier}
cmu-medical-scanner-bleeding-tier = Кровотечение: {$tier}
cmu-medical-scanner-bleed-tier-minor = Незначительное
cmu-medical-scanner-bleed-tier-moderate = Умеренное
cmu-medical-scanner-bleed-tier-severe = Сильное
cmu-medical-scanner-bleed-tier-arterial = Артериальное
cmu-medical-scanner-chip-stump-bleeding = Культя: {$tier} кровотечение
cmu-medical-scanner-chip-stump-clamped = Культя: жгут

cmu-medical-scanner-label-health = Здоровье:

cmu-medical-scanner-label-damage = Повреждения:

cmu-medical-scanner-label-holocard = Голокарта:

cmu-medical-scanner-label-temperature = Температура тела:

cmu-medical-scanner-label-pulse = Пульс:

cmu-medical-scanner-label-shock-risk = Риск шока:

cmu-medical-scanner-section-limbs-damaged = Повреждения конечностей

cmu-medical-scanner-section-organs-damaged = Повреждения органов

cmu-medical-scanner-section-chemicals = Химические вещества

cmu-medical-scanner-section-advice = Рекомендации по лечению

cmu-medical-scanner-organ-damage = {$stage} [урон: {$damage}]
