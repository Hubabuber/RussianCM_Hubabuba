# V2-β surgery UX strings.
# - Window header / hint
# - Armed-step status line
# - Wrong-tool / wrong-part / no-tool popups
# - Tool category names (resolver categories from SharedCMUSurgeryFlowSystem)
# - Per-step labels for all 19 V1 CMU surgeries

# ---- Window chrome ---------------------------------------------------

cmu-medical-surgery-window-title = Хирургическая процедура
cmu-medical-surgery-window-hint = Выберите часть тела, выберите операцию и используйте нужный инструмент на пациенте.
cmu-medical-surgery-no-eligible = Здесь нет доступных операций.
cmu-medical-surgery-section-patient = Пациент
cmu-medical-surgery-section-workflow = Процесс
cmu-medical-surgery-workflow-ready = Активная процедура не выбрана.
cmu-medical-surgery-workflow-active = { $surgery } активна на { $part }.
cmu-medical-surgery-section-parts = Части тела
cmu-medical-surgery-section-surgeries = Операции
cmu-medical-surgery-section-surgeries-on = Операции на { $part }
cmu-medical-surgery-no-part-selected = Выберите часть тела.
cmu-medical-surgery-procedure-detail = { $step } / { $tool }
cmu-medical-surgery-arm-button = Начать операцию
cmu-medical-surgery-cancel-armed = Отменить операцию
cmu-medical-surgery-step-hint = Шаг { $step }/{ $total } — { $label } ({ $tool })
cmu-medical-surgery-step-hint-prereq = Предварительный шаг { $step }/{ $total } — { $label } ({ $tool })
cmu-medical-surgery-armed-heading = АКТИВИРОВАНО

# ---- In-progress hero panel ------------------------------------------

cmu-medical-surgery-in-progress-heading = В ПРОЦЕССЕ
cmu-medical-surgery-in-progress-subtitle = { $surgery } · { $part }
cmu-medical-surgery-in-progress-credit = Начато { $surgeon } · { $elapsed } назад
cmu-medical-surgery-step-now = Шаг { $step }: { $label }
cmu-medical-surgery-action-hint = Нажмите { $part } с { $tool }.
cmu-medical-surgery-action-hint-no-tool = Нажмите { $part }, чтобы продолжить.
cmu-medical-surgery-choose-next-heading = Выберите следующую операцию
cmu-medical-surgery-choose-next-hint = Продолжите восстановление или закройте разрезы.
cmu-medical-surgery-continue-with-button = Продолжить { $surgery }
cmu-medical-surgery-close-up-button = Закрыть
cmu-medical-surgery-continue-button = Продолжить операцию
cmu-medical-surgery-abandon-button = Прервать операцию
cmu-medical-surgery-actions-heading = Действия

# ---- Per-part section labels -----------------------------------------

cmu-medical-surgery-part-heading = { $part }
cmu-medical-surgery-part-condition-healthy = Здоровый
cmu-medical-surgery-part-condition-locked = Другая операция выполняется на { $other } — завершите или отмените её
cmu-medical-surgery-part-condition-no-eligible = Нет доступных операций

cmu-medical-surgery-condition-incision-open = Открытый разрез
cmu-medical-surgery-condition-ribcage-open = Открытая грудная клетка
cmu-medical-surgery-condition-skull-open = Открытый череп
cmu-medical-surgery-condition-bones-open = Открытые кости
cmu-medical-surgery-condition-fracture = { $severity } перелом
cmu-medical-surgery-condition-internal-bleed = Внутреннее кровотечение
cmu-medical-surgery-condition-eschar = Струп
cmu-medical-surgery-condition-in-progress = Идёт операция
cmu-medical-surgery-condition-missing = Ампутировано

# ---- BUI category headers ---------------------------------------------

cmu-medical-surgery-category-fracture = Переломы
cmu-medical-surgery-category-bleed = Внутреннее кровотечение
cmu-medical-surgery-category-burn = Ожоги
cmu-medical-surgery-category-remove_organ = Удаление органа
cmu-medical-surgery-category-transplant = Трансплантация органа
cmu-medical-surgery-category-suture = Ушивание органа
cmu-medical-surgery-category-head_organ = Операции на голове
cmu-medical-surgery-category-amputation = Ампутация конечности
cmu-medical-surgery-category-reattach = Пришивание конечности
cmu-medical-surgery-category-parasite = Удаление паразита
cmu-medical-surgery-category-close_up = Завершение
cmu-medical-surgery-category-general = Прочее

# ---- Examine surface (CMUSurgeryStateExamineSystem) ------------------

cmu-medical-surgery-examine-patient-in-progress = [color=#dca94c]{ $surgery } в процессе (выполняет { $surgeon }) — следующий: { $next }.[/color]
cmu-medical-surgery-examine-part-in-progress = [color=#dca94c]{ $surgery } в процессе (выполняет { $surgeon }) — следующий: { $next }.[/color]
cmu-medical-surgery-examine-part-abandoned = [color=#888888]Открытая рана — операция не выполняется.[/color]

# ---- Close-up step labels (RMC fallback resolution) ------------------

cmu-medical-surgery-step-close-incision-label = Закрыть разрез
cmu-medical-surgery-step-mend-ribcage-label = Срастить грудную клетку
cmu-medical-surgery-step-mend-skull-label = Срастить череп
cmu-medical-surgery-step-mend-bones-label = Срастить кости
cmu-medical-surgery-step-close-bones-label = Закрыть кости

# ---- Armed-step status -----------------------------------------------

cmu-medical-surgery-armed-none = (операция не выбрана)
cmu-medical-surgery-armed-step = Активно: { $surgery } — шаг { $step } ({ $tool })
cmu-medical-surgery-armed-cancelled = Операция отменена.
cmu-medical-surgery-armed-expired = Выбор операции истёк.
cmu-medical-surgery-auto-armed = Выбрано { $surgery }.
cmu-medical-surgery-auto-continue = Продолжение { $surgery }.
cmu-medical-surgery-choose-repair-or-close = Выберите восстановление или закрытие.

# ---- Click-target popups ---------------------------------------------

cmu-medical-surgery-wrong-part = Это не та часть, на которую назначена операция.
cmu-medical-surgery-wrong-tool = Неверный инструмент для этого шага.
cmu-medical-surgery-wrong-tool-damage = Вы промахиваетесь с { $tool }!
cmu-medical-surgery-improvised-mishap = Импровизированный { $tool } срывается и вызывает травму.
cmu-medical-surgery-step-failed = Операция срывается и вызывает травму.
cmu-medical-surgery-step-failed-with-tool = { $tool } срывается и вызывает травму.
cmu-medical-surgery-no-tool = Для этого шага нужен хирургический инструмент.
cmu-medical-surgery-missing-skills = Вы не умеете выполнять этот шаг.
cmu-medical-surgery-cannot-start = Эта операция больше недоступна.
cmu-medical-surgery-needs-operating-table = Сначала поместите пациента на стол.
cmu-medical-surgery-remove-helmet = Сначала снимите шлем.
cmu-medical-surgery-remove-armor = Сначала снимите броню.
cmu-medical-surgery-wrong-limb = Эта конечность не подходит пациенту.
cmu-medical-surgery-welder-not-lit = Сначала зажгите инструмент.
cmu-medical-surgery-patient-not-lying = Пациент должен лежать или быть зафиксирован.
cmu-medical-surgery-patient-not-controlled = Нужна анестезия, обезболивание или фиксация.
cmu-medical-surgery-self-pain-control = Для самохирургии нужны сильные обезболивающие.
cmu-medical-surgery-self-not-secured = Зафиксируйте себя перед самохирургией.
cmu-medical-surgery-self-not-allowed = Вы не можете выполнить эту операцию на себе.
cmu-medical-surgery-step-pain-interrupted = Боль пациента прерывает операцию.
cmu-medical-amputation-success = Конечность удалена.

# ---- Tool category names (used in the BUI button + armed line) -------

cmu-medical-surgery-tool-category-scalpel = Скальпель
cmu-medical-surgery-tool-category-hemostat = Гемостат
cmu-medical-surgery-tool-category-retractor = Ретрактор
cmu-medical-surgery-tool-category-cautery = Прижигатель
cmu-medical-surgery-tool-category-bone_saw = Костная пила
cmu-medical-surgery-tool-category-bone_setter = Костный фиксатор
cmu-medical-surgery-tool-category-bone_gel = Костный гель
cmu-medical-surgery-tool-category-bone_graft = Костный трансплантат
cmu-medical-surgery-tool-category-organ_clamp = Органный зажим
cmu-medical-surgery-tool-category-scalpel_or_burn_kit = Скальпель или набор для ожогов
cmu-medical-surgery-tool-category-severed_limb = Подходящая конечность
cmu-medical-surgery-tool-category-blowtorch = Зажжённый сварочный аппарат
cmu-medical-surgery-tool-category-cable_coil = Кабельная катушка

# ---- Per-step labels -------------------------------------------------

cmu-medical-surgery-step-realign-simple-label = Вправить простой перелом
cmu-medical-surgery-step-realign-compound-label = Вправить сложный перелом
cmu-medical-surgery-step-realign-comminuted-label = Вправить оскольчатый перелом
cmu-medical-surgery-step-apply-gel-label = Нанести костный гель
cmu-medical-surgery-step-apply-gel-second-label = Нанести костный гель (второй слой)
cmu-medical-surgery-step-insert-graft-label = Установить костный трансплантат
cmu-medical-surgery-step-cauterize-bleed-label = Пережать внутреннее кровотечение
cmu-medical-surgery-step-clamp-liver-label = Пережать сосуды печени
cmu-medical-surgery-step-clamp-lungs-label = Пережать сосуды лёгких
cmu-medical-surgery-step-clamp-kidneys-label = Пережать сосуды почек
cmu-medical-surgery-step-clamp-heart-label = Пережать сосуды сердца
cmu-medical-surgery-step-clamp-stomach-label = Пережать сосуды желудка
cmu-medical-surgery-step-extract-liver-label = Извлечь печень
cmu-medical-surgery-step-extract-lungs-label = Извлечь лёгкие
cmu-medical-surgery-step-extract-kidneys-label = Извлечь почки
cmu-medical-surgery-step-extract-heart-label = Извлечь сердце
cmu-medical-surgery-step-extract-stomach-label = Извлечь желудок
cmu-medical-surgery-step-reinsert-liver-label = Установить замену печени
cmu-medical-surgery-step-reinsert-lungs-label = Установить замену лёгких
cmu-medical-surgery-step-reinsert-kidneys-label = Установить замену почек
cmu-medical-surgery-step-reinsert-stomach-label = Установить замену желудка
cmu-medical-surgery-step-transplant-heart-label = Пересадить донорское сердце
cmu-medical-surgery-step-suture-liver-label = Ушить печень
cmu-medical-surgery-step-suture-lungs-label = Ушить лёгкие
cmu-medical-surgery-step-suture-kidneys-label = Ушить почки
cmu-medical-surgery-step-suture-heart-label = Ушить сердце
cmu-medical-surgery-step-suture-stomach-label = Ушить желудок
cmu-medical-surgery-step-amputate-limb-label = Ампутировать конечность
cmu-medical-surgery-step-reattach-limb-label = Пришить оторванную конечность
cmu-medical-surgery-step-trim-necrotic-stump-label = Обрезать некротическую культю
cmu-medical-surgery-step-prep-reattach-socket-label = Подготовить место рефиксации
cmu-medical-surgery-step-debride-eschar-label = Удалить струп

# ---- Surgery names ---------------------------------------------------

cmu-medical-surgery-name-set-fracture = Стабилизация перелома
cmu-medical-surgery-name-stop-internal-bleeding = Остановка внутреннего кровотечения
cmu-medical-surgery-name-remove-liver = Удаление печени
cmu-medical-surgery-name-remove-lungs = Удаление лёгких
cmu-medical-surgery-name-remove-kidneys = Удаление почек
cmu-medical-surgery-name-remove-heart = Удаление сердца
cmu-medical-surgery-name-remove-stomach = Удаление желудка
cmu-medical-surgery-name-replace-liver = Замена печени
cmu-medical-surgery-name-replace-lungs = Замена лёгких
cmu-medical-surgery-name-replace-kidneys = Замена почек
cmu-medical-surgery-name-transplant-heart = Трансплантация сердца
cmu-medical-surgery-name-replace-stomach = Замена желудка
cmu-medical-surgery-name-suture-liver = Ушивание печени
cmu-medical-surgery-name-suture-lungs = Ушивание лёгких
cmu-medical-surgery-name-suture-kidneys = Ушивание почек
cmu-medical-surgery-name-suture-heart = Ушивание сердца
cmu-medical-surgery-name-suture-stomach = Ушивание желудка
cmu-medical-surgery-name-repair-brain = Восстановление мозга
cmu-medical-surgery-name-repair-eyes = Восстановление глаз
cmu-medical-surgery-name-repair-ears = Восстановление ушей
cmu-medical-surgery-name-remove-limb = Ампутация конечности
cmu-medical-surgery-name-reattach-limb = Пришивание конечности
cmu-medical-surgery-name-remove-larva = Удаление личинки
cmu-medical-surgery-name-debride-eschar = Удаление струпа

# ---- Autodoc ---------------------------------------------------------

cmu-autodoc-window-title = Автодок
cmu-autodoc-no-patient = Нет пациента
cmu-autodoc-status-no-pod = Нет подключённой капсулы автодока.
cmu-autodoc-status-empty = Подключённая капсула пуста.
cmu-autodoc-status-ready = Готов к автоматическим процедурам.
cmu-autodoc-status-running = Выполняются автоматические процедуры.
cmu-autodoc-current-idle = Текущая процедура: ожидание
cmu-autodoc-current-step = Текущая процедура: { $step }
cmu-autodoc-current-step-timed = Текущая процедура: { $step } ({ $time } осталось)
cmu-autodoc-current-step-detail = { $surgery } / { $part } / { $step }
cmu-autodoc-start-button = Старт
cmu-autodoc-stop-button = Стоп
cmu-autodoc-clear-button = Очистить
cmu-autodoc-eject-button = Извлечь пациента
cmu-autodoc-remove-button = Удалить
cmu-autodoc-queue-button = В очередь
cmu-autodoc-queue-heading = Очередь
cmu-autodoc-parts-heading = Части тела
cmu-autodoc-surgeries-heading = Операции
cmu-autodoc-queue-empty = Очередь пуста.
cmu-autodoc-queue-summary = { $count } процедур в очереди
cmu-autodoc-available-procedures = { $count } доступных процедур
cmu-autodoc-part-procedures = { $count } процедур(ы)
cmu-autodoc-surgery2-required = Требуется обучение Surgery 2 для очереди автодока.
cmu-autodoc-no-surgeries = Нет доступных операций.
cmu-autodoc-queue-row = #{ $index } { $surgery } на { $part } - { $step }
cmu-autodoc-surgery-row = { $surgery } - { $step }
cmu-autodoc-automated-step-label = Автоматический цикл лечения
cmu-autodoc-automated-step-note = Автодок выполняет лечение по таймеру.
cmu-autodoc-repair-wounds-surgery = Лечение ран / ожогов
cmu-autodoc-procedure-time-note = { $time } автоматическая процедура.
cmu-autodoc-minutes = { $minutes } мин

# ---- Body scanner ----------------------------------------------------

cmu-body-scanner-window-title = Сканер тела
cmu-body-scanner-no-patient = Нет пациента
cmu-body-scanner-status-no-pod = Нет подключённой капсулы сканера.
cmu-body-scanner-status-empty = Подключённая капсула пуста.
cmu-body-scanner-status-ready = Сканирование пациента готово.
cmu-body-scanner-status-no-skill = Требуется обучение Surgery 1 для сканирования.
cmu-body-scanner-boost-active = Хирургическая помощь откалибрована: осталось { $time }.
cmu-body-scanner-boost-inactive = Хирургическая помощь не откалибрована.
cmu-body-scanner-scan-heading = Скан
cmu-body-scanner-terms-heading = Срезы слоёв
cmu-body-scanner-targets-heading = Активные сигналы
cmu-body-scanner-start-button = Начать калибровку
cmu-body-scanner-reset-button = Сбросить калибровку
cmu-body-scanner-eject-button = Извлечь пациента
cmu-body-scanner-surgery1-required = Требуется Surgery 1 для сканирования.
cmu-body-scanner-no-scan-lines = Нет данных сканирования.
cmu-body-scanner-diagnostic-summary = { $count } диагностических линий
cmu-body-scanner-match-summary = { $matched }/{ $required } зафиксировано, осталось { $time }
cmu-body-scanner-match-summary-idle = { $matched }/{ $required } зафиксировано, не начато
cmu-body-scanner-calibrated-summary = Откалибровано, осталось { $time } ассиста
cmu-body-scanner-calibrated-badge = КАЛИБРОВКА { $time }
cmu-body-scanner-calibration-ready = 2:00
cmu-body-scanner-lockout-summary = Слой заблокирован, осталось { $time }
cmu-body-scanner-lockout-status = Слой заблокирован: осталось { $time }.
cmu-body-scanner-lockout-detail = Калибровка провалена. Дождитесь снятия блокировки.
cmu-body-scanner-no-surgical-targets = Цели не обнаружены.
cmu-body-scanner-no-surgical-targets-detail = Бонус не начислен.
cmu-body-scanner-calibration-heading = Скан анатомических слоёв
cmu-body-scanner-sweep-title = Послойное сканирование
cmu-body-scanner-sweep-detail = Настройте слой для начала.
cmu-body-scanner-layer-selected = Слой настроен — { $locked }/{ $total } зафиксировано
cmu-body-scanner-layer-ready = { $locked }/{ $total } зафиксировано
cmu-body-scanner-layer-empty = Аномалий не обнаружено
cmu-body-scanner-signal-locked = Сигнал зафиксирован
cmu-body-scanner-signal-ready = { $detail } — захват при входе в голубую зону
cmu-body-scanner-start-status = Начните калибровку для запуска сканирования.
cmu-body-scanner-ready-status = Настройте слой и фиксируйте сигналы в голубой зоне.
cmu-body-scanner-armed-status = Слой настроен: { $layer }. Фиксируйте сигналы в голубой зоне.
cmu-body-scanner-penalty-status = Ошибка времени или слоя: -{ $seconds }с.
cmu-body-scanner-feedback-correct = Сигнал зафиксирован.
cmu-body-scanner-feedback-wrong-timing = Пропущена зона захвата: -{ $seconds }с.
cmu-body-scanner-feedback-wrong-layer = Помехи слоя: -{ $seconds }с.
cmu-body-scanner-expired-status = Время истекло. Сбросьте калибровку.
cmu-body-scanner-complete-status = Все сигналы зафиксированы. Калибровка завершена.
cmu-body-scanner-timer-active = АКТИВНОЕ СКАНИРОВАНИЕ
cmu-body-scanner-timer-expired = ВРЕМЯ ИСТЕКЛО
cmu-body-scanner-timer-locked = СЛОЙ ЗАФИКСИРОВАН
cmu-body-scanner-timer-detail = Зафиксируйте сигналы до закрытия окна.
cmu-body-scanner-no-layer-signals = Нет аномалий на { $layer }.
cmu-body-scanner-interference-title = Неразрешённый сигнал
cmu-body-scanner-interference-detail = Помехи на { $layer }
cmu-body-scanner-decoy-ready = { $detail } — шумовое эхо
cmu-body-scanner-decoy-vitals-1 = Кардиальный всплеск эха
cmu-body-scanner-decoy-vitals-2 = Колебание кислорода крови
cmu-body-scanner-decoy-detail-vitals = временный витальный артефакт
cmu-body-scanner-decoy-skeleton-1 = Тонкая тень кости
cmu-body-scanner-decoy-skeleton-2 = Призрак суставного выравнивания
cmu-body-scanner-decoy-detail-skeleton = нестабильный костный силуэт
cmu-body-scanner-decoy-organs-1 = Размытый органический отклик
cmu-body-scanner-decoy-organs-2 = Отражение плотности
cmu-body-scanner-decoy-detail-organs = нестабильная плотность органов
cmu-body-scanner-decoy-tissue-1 = Поверхностная вспышка ткани
cmu-body-scanner-decoy-tissue-2 = Полоса сосудистого шума
cmu-body-scanner-decoy-detail-tissue = шумовой отклик мягких тканей
cmu-body-scanner-triage-stable = Стабильно
cmu-body-scanner-triage-serious = Серьёзные изменения
cmu-body-scanner-triage-critical = Критическое состояние
cmu-body-scanner-triage-clear = Немедленных отклонений не обнаружено.
cmu-body-scanner-health-stable = Стабильно
cmu-body-scanner-health-damaged = Повреждено
cmu-body-scanner-health-critical = Критично
cmu-body-scanner-section-vitals = Витальные показатели
cmu-body-scanner-section-body = Тело
cmu-body-scanner-section-organs = Органы
cmu-body-scanner-term-assigned = { $term } -> { $target }
cmu-body-scanner-target-filled = { $target }: { $term }
cmu-body-scanner-line-state = Состояние: { $state }
cmu-body-scanner-line-damage = Урон: всего { $total } (brute { $brute }, burn { $burn })
cmu-body-scanner-line-blood = Кровь: { $blood } / { $max }
cmu-body-scanner-heart-stopped = Сердце: нет активности
cmu-body-scanner-heart-active = Сердце: { $bpm } уд/мин
cmu-body-scanner-line-no-data = Нет диагностических данных.
cmu-body-scanner-line-part = { $part }: { $details }
cmu-body-scanner-part-health = HP { $current } / { $max }
cmu-body-scanner-part-wounds = { $count } незалеченных ран
cmu-body-scanner-part-fracture = { $severity } перелом
cmu-body-scanner-part-bleed = внутреннее кровотечение { $rate }/с
cmu-body-scanner-part-eschar = струп
cmu-body-scanner-part-splinted = зафиксировано шиной
cmu-body-scanner-part-cast = в гипсе
cmu-body-scanner-part-tourniquet = наложен жгут
cmu-body-scanner-part-missing-limb = конечность отсутствует / ампутирована
cmu-body-scanner-line-organ = { $organ }: { $stage } ({ $current } / { $max })
cmu-body-scanner-line-missing-organ = Отсутствует { $organ } в { $part }
cmu-body-scanner-signal-heart-stopped = Сердце: нет активности
cmu-body-scanner-signal-organ-damage = { $organ }: стадия повреждения { $stage }
cmu-body-scanner-signal-low-blood = Низкий объём крови: { $blood } / { $max }
cmu-body-scanner-signal-internal-bleed = { $part }: внутреннее кровотечение { $rate }/с
cmu-body-scanner-signal-fracture = { $part }: { $severity } перелом
cmu-body-scanner-signal-wounds = { $part }: { $count } незалеченных ран
cmu-body-scanner-signal-trauma = { $part }: травма тканей { $current } / { $max }
cmu-body-scanner-signal-missing-organ = Отсутствует { $organ } в { $part }
cmu-body-scanner-signal-missing-limb = { $part }: конечность отсутствует / ампутирована
cmu-body-scanner-slice-detail-cardiac = сердечный ритм
cmu-body-scanner-slice-detail-organ = плотность органа
cmu-body-scanner-slice-detail-blood = объём крови
cmu-body-scanner-slice-detail-bleed = тканевый кровоток
cmu-body-scanner-slice-detail-fracture = выравнивание костей
cmu-body-scanner-slice-detail-wound = повреждение тканей
cmu-body-scanner-slice-detail-trauma = плотность мягких тканей
cmu-body-scanner-slice-detail-missing-organ = силуэт органа
cmu-body-scanner-slice-detail-missing-limb = силуэт конечности

cmu-limb-printer-window-title = Принтер конечностей
cmu-limb-printer-header = Создание конечностей
cmu-limb-printer-matrix-heading = Матрица синтеза
cmu-limb-printer-blood-heading = Кровяной шаблон
cmu-limb-printer-no-beaker = Не вставлен стакан с матрицей.
cmu-limb-printer-no-syringe = Не вставлен шприц с кровью.
cmu-limb-printer-fluid-amount = { $current } / { $max }u
cmu-limb-printer-matrix-cost = { $cost }u матрицы на печать
cmu-limb-printer-blood-cost = { $cost }u крови на печать
cmu-limb-printer-remove-beaker = Извлечь стакан
cmu-limb-printer-remove-syringe = Извлечь шприц
cmu-limb-printer-left-heading = Лево
cmu-limb-printer-right-heading = Право
cmu-limb-printer-print-ready = Готово к печати
cmu-limb-printer-status-ready = Готов к синтезу.
cmu-limb-printer-missing-beaker = Вставьте стакан с биоматрицей.
cmu-limb-printer-missing-matrix = Недостаточно биоматрицы.
cmu-limb-printer-missing-syringe = Вставьте шприц с кровью пациента.
cmu-limb-printer-missing-blood = Недостаточно образца крови.
cmu-limb-printer-printed = Напечатано: { $limb }.
cmu-limb-printer-left-arm = Левая рука
cmu-limb-printer-right-arm = Правая рука
cmu-limb-printer-left-leg = Левая нога
cmu-limb-printer-right-leg = Правая нога
cmu-limb-printer-slot-beaker = стакан с матрицей
cmu-limb-printer-slot-syringe = шприц с кровью
