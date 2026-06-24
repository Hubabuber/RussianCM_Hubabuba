from __future__ import annotations

import html
import json
import re
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GUIDEBOOK = ROOT / "Resources" / "ServerInfo" / "Guidebook"
OUTPUT = ROOT / "Resources" / "Prototypes" / "_RuMC14" / "RoleTests" / "questions.yml"

COMMON_COUNT = 100
LAW_COUNT = 50
ROLE_COUNT = 20


@dataclass(frozen=True)
class Fact:
    text: str
    source: str
    heading: str


RULES = GUIDEBOOK / "_AU14" / "Rules"
UCMJ = GUIDEBOOK / "_AU14" / "UCMJ"
SOP = GUIDEBOOK / "_AU14" / "SOP"
RUMC_GUIDES = GUIDEBOOK / "_RuMC14" / "Guides"
PROTOTYPE_ROOTS = [
    ROOT / "Resources" / "Prototypes" / "_AU14",
    ROOT / "Resources" / "Prototypes" / "_RMC14",
    ROOT / "Resources" / "Prototypes" / "_CMU14",
]
RU_LOCALE = ROOT / "Resources" / "Locale" / "ru-RU"

COMMON_SOURCES = [
    RULES / "AU14GeneralRules.xml",
    RULES / "AU14RoleplayRules.xml",
    RULES / "AU14ZeroTolerance.xml",
    RULES / "AU14HighRoleplayRoles.xml",
    *sorted((RULES / "Gamemodes").glob("*.xml")),
]

LAW_SOURCES = sorted(UCMJ.rglob("*.xml"))

ROLE_LABELS = {
    "AU14JobCivilianAmbassadorCCA": "посол ЦКА",
    "AU14JobCivilianAmbassadorICSC": "посол МККС",
    "AU14JobCivilianAmbassadorTWE": "посол Третьей мировой империи",
    "AU14JobCivilianAmbassadorUA": "посол Объединённых Америк",
    "AU14JobCivilianAmbassadorUPP": "посол Союза прогрессивных народов",
    "AU14JobCivilianColonyAdminAssistant": "помощник администрации колонии",
    "AU14JobCivilianColonyAdministrator": "администратор колонии",
    "AU14JobCivilianColonySynthetic": "синтетик колонии",
    "AU14JobCivilianCorporateAssistant": "корпоративный помощник",
    "AU14JobCivilianCorporateLiaison": "корпоративный представитель",
    "AU14JobCivilianEmergencyResponseOfficer": "офицер экстренного реагирования",
    "AU14JobCivilianEngineer": "инженер колонии",
    "AU14JobCivilianEthicsAndWellnessAdvisor": "советник по этике и благополучию",
    "AU14JobCivilianFoodServiceWorker": "работник общественного питания",
    "AU14JobCivilianForeman": "бригадир",
    "AU14JobCivilianFreightSystemsSpecialist": "специалист по грузовым системам",
    "AU14JobCivilianHeadOfEngineering": "главный инженер колонии",
    "AU14JobCivilianHeadOfService": "руководитель сервисного отдела",
    "AU14JobCivilianHeadPhysician": "главный врач колонии",
    "AU14JobCivilianJournalist": "журналист",
    "AU14JobCivilianKellandWarden": "смотритель Келланда",
    "AU14JobCivilianNSPAConstable": "констебль NSPA",
    "AU14JobCivilianNurse": "медсестра колонии",
    "AU14JobCivilianOrbitalArbiter": "орбитальный арбитр",
    "AU14JobCivilianOrbitalLawyer": "орбитальный юрист",
    "AU14JobCivilianPhysician": "врач колонии",
    "AU14JobCivilianPrisoner": "заключённый колонии",
    "AU14JobCivilianScientist": "учёный колонии",
    "AU14JobCivilianShopkeep": "владелец магазина",
    "AU14JobCivilianUSASFRecruiter": "вербовщик USASF",
    "AU14JobCivilianWasteManagementSpecialist": "специалист по утилизации отходов",
    "AU14JobCLFGuerilla": "партизан CLF",
    "AU14JobColonyWorkingJoe": "Working Joe колонии",
    "AU14JobGOVFORPlatCo": "командир взвода GOVFOR",
    "AU14JobMobBoss": "глава преступной группировки",
    "AU14JobMobGoon": "участник преступной группировки",
    "AU14JobOpforPlatCo": "командир взвода OPFOR",
    "AU14JobThirdPartyLeader": "лидер третьей стороны",
    "AU14JobThreatLeader": "лидер угрозы",
    "AU14JobWYGuard": "корпоративный охранник",
    "CMProvostInspector": "инспектор военной полиции",
}

ROLE_GROUPS = {
    "ambassador": [
        RULES / "AU14RoleplayRules.xml",
        RULES / "AU14GeneralRules.xml",
        SOP / "AU14SOPThirdParty.xml",
    ],
    "command": [
        RULES / "AU14HighRoleplayRoles.xml",
        SOP / "AI14SOPCommand.xml",
        SOP / "AU14SOPRankStructure.xml",
        RUMC_GUIDES / "RMCGuideCommandRoles.xml",
    ],
    "synthetic": [
        RULES / "AU14HighRoleplayRoles.xml",
        RULES / "AU14RoleplayRules.xml",
    ],
    "corporate": [
        RULES / "AU14GeneralRules.xml",
        RULES / "AU14RoleplayRules.xml",
        SOP / "AU14SOPThirdParty.xml",
    ],
    "security": [
        UCMJ / "AU14UCMJArrestingProcedure.xml",
        UCMJ / "AU14UCMJEnforcementStructure.xml",
        UCMJ / "AI14UCMJDefinitionsandGlossary.xml",
        SOP / "AI14SOPRulesofEngagement.xml",
    ],
    "engineering": [
        SOP / "AI14SOPEquipmentAndPersonnel.xml",
        RUMC_GUIDES / "RMCGuideCommandRoles.xml",
    ],
    "medical": [
        RUMC_GUIDES / "RuMCMedical.xml",
        SOP / "AI14SOPEquipmentAndPersonnel.xml",
    ],
    "service": [
        RULES / "AU14GeneralRules.xml",
        RULES / "AU14RoleplayRules.xml",
        SOP / "AI14SOPEquipmentAndPersonnel.xml",
    ],
    "legal": [
        *LAW_SOURCES,
    ],
    "military": [
        SOP / "AI14SOPCommand.xml",
        SOP / "AI14SOPRulesofEngagement.xml",
        SOP / "AI14SOPEquipmentAndPersonnel.xml",
        RULES / "AU14HighRoleplayRoles.xml",
        RULES / "AU14RoleplayRules.xml",
    ],
    "criminal": [
        RULES / "AU14GeneralRules.xml",
        RULES / "AU14RoleplayRules.xml",
        RULES / "Gamemodes" / "AU14GamemodeRulesInsurgency.xml",
    ],
    "civilian": [
        RULES / "AU14GeneralRules.xml",
        RULES / "AU14RoleplayRules.xml",
        SOP / "AU14SOPThirdParty.xml",
    ],
    "science": [
        RUMC_GUIDES / "RuMCIntel.xml",
        RUMC_GUIDES / "RuMCMedical.xml",
    ],
    "logistics": [
        RUMC_GUIDES / "RuMCIntel.xml",
        SOP / "AI14SOPEquipmentAndPersonnel.xml",
    ],
}

ROLE_TO_GROUP = {
    **{role: "ambassador" for role in ROLE_LABELS if "Ambassador" in role},
    "AU14JobCivilianColonyAdminAssistant": "command",
    "AU14JobCivilianColonyAdministrator": "command",
    "AU14JobCivilianColonySynthetic": "synthetic",
    "AU14JobCivilianCorporateAssistant": "corporate",
    "AU14JobCivilianCorporateLiaison": "corporate",
    "AU14JobCivilianEmergencyResponseOfficer": "security",
    "AU14JobCivilianEngineer": "engineering",
    "AU14JobCivilianEthicsAndWellnessAdvisor": "legal",
    "AU14JobCivilianFoodServiceWorker": "service",
    "AU14JobCivilianForeman": "engineering",
    "AU14JobCivilianFreightSystemsSpecialist": "logistics",
    "AU14JobCivilianHeadOfEngineering": "engineering",
    "AU14JobCivilianHeadOfService": "service",
    "AU14JobCivilianHeadPhysician": "medical",
    "AU14JobCivilianJournalist": "civilian",
    "AU14JobCivilianKellandWarden": "security",
    "AU14JobCivilianNSPAConstable": "security",
    "AU14JobCivilianNurse": "medical",
    "AU14JobCivilianOrbitalArbiter": "legal",
    "AU14JobCivilianOrbitalLawyer": "legal",
    "AU14JobCivilianPhysician": "medical",
    "AU14JobCivilianPrisoner": "criminal",
    "AU14JobCivilianScientist": "science",
    "AU14JobCivilianShopkeep": "service",
    "AU14JobCivilianUSASFRecruiter": "military",
    "AU14JobCivilianWasteManagementSpecialist": "service",
    "AU14JobCLFGuerilla": "criminal",
    "AU14JobColonyWorkingJoe": "synthetic",
    "AU14JobGOVFORPlatCo": "military",
    "AU14JobMobBoss": "criminal",
    "AU14JobMobGoon": "criminal",
    "AU14JobOpforPlatCo": "military",
    "AU14JobThirdPartyLeader": "command",
    "AU14JobThreatLeader": "command",
    "AU14JobWYGuard": "security",
    "CMProvostInspector": "security",
}


def clean_markup(value: str) -> str:
    value = re.sub(r'\[textlink="([^"]+)"[^\]]*\]', r"\1", value)
    value = re.sub(r"<[^>]+>", " ", value)
    value = re.sub(r"\[[^\]]+\]", "", value)
    value = html.unescape(value)
    value = value.replace("\\n", " ")
    value = re.sub(r"\s+", " ", value)
    return value.strip(" \t\r\n-*")


def source_name(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def extract_facts(paths: list[Path]) -> list[Fact]:
    facts: list[Fact] = []
    seen: set[str] = set()

    for path in paths:
        if not path.exists():
            continue

        heading = path.stem
        for raw_line in path.read_text(encoding="utf-8-sig").splitlines():
            stripped = raw_line.strip()
            if stripped.startswith("#"):
                heading = clean_markup(stripped.lstrip("#"))
                continue

            text = clean_markup(stripped)
            if not 35 <= len(text) <= 320:
                continue
            if len(re.findall(r"[А-Яа-яЁё]", text)) < 15:
                continue
            if text.endswith(":") or text.lower().startswith(("document", "примечание:")):
                continue

            key = re.sub(r"\W+", "", text).lower()
            if key in seen:
                continue
            seen.add(key)
            facts.append(Fact(text, source_name(path), heading))

    return facts


def load_localizations() -> tuple[dict[str, str], dict[str, str]]:
    values: dict[str, str] = {}
    sources: dict[str, str] = {}
    for path in RU_LOCALE.rglob("*.ftl"):
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            match = re.match(r"^([A-Za-z0-9_-]+)\s*=\s*(.+)$", line)
            if not match:
                continue
            key, value = match.groups()
            values[key] = clean_markup(value)
            sources[key] = source_name(path)
    return values, sources


def load_role_description_facts() -> dict[str, Fact]:
    localizations, localization_sources = load_localizations()
    descriptions: dict[str, Fact] = {}

    for root in PROTOTYPE_ROOTS:
        for path in root.rglob("*.yml"):
            text = path.read_text(encoding="utf-8-sig")
            for block in re.split(r"(?m)^- type: ", text):
                if not block.startswith("job\n") and not block.startswith("job\r\n"):
                    continue
                id_match = re.search(r"(?m)^  id: (\S+)", block)
                description_match = re.search(r"(?m)^  description: (\S+)", block)
                if not id_match or not description_match:
                    continue

                role_id = id_match.group(1)
                description_key = description_match.group(1)
                description = localizations.get(description_key)
                if role_id not in ROLE_LABELS or not description:
                    continue

                descriptions[role_id] = Fact(
                    description,
                    localization_sources[description_key],
                    f"Описание роли «{ROLE_LABELS[role_id]}»",
                )

    return descriptions


def replace_once(text: str, replacements: list[tuple[str, object]]) -> str | None:
    for old, new in replacements:
        match = re.search(old, text, flags=re.IGNORECASE)
        if match:
            replacement = new(match) if callable(new) else str(new)
            if match.group(0)[0].isupper() and replacement:
                replacement = replacement[0].upper() + replacement[1:]
            return text[:match.start()] + replacement + text[match.end():]
    return None


def make_distractors(correct: str) -> list[str]:
    variants: list[str] = []
    replacement_sets = [
        [
            (r"\bне могут\b", "могут"),
            (r"\bне может\b", "может"),
            (r"\bне должны\b", "должны"),
            (r"\bне должен\b", "должен"),
            (r"\bобязаны\b", "могут"),
            (r"\bобязан\b", "может"),
            (r"\bдолжны\b", "могут"),
            (r"\bдолжен\b", "может"),
            (r"\bзапрещено\b", "разрешено"),
            (r"\bзапрещены\b", "разрешены"),
            (r"\bразрешено\b", "запрещено"),
            (r"\bможет\b", "обязан"),
            (r"\bмогут\b", "обязаны"),
        ],
        [
            (r"\bкак минимум\b", "не более чем"),
            (r"\bне менее\b", "не более"),
            (r"\bне более\b", "не менее"),
            (r"\bтолько\b", "в любых случаях"),
            (r"\bвсегда\b", "только по приказу"),
            (r"\bнемедленно\b", "после окончания операции"),
            (r"\bдо завершения\b", "после завершения"),
            (r"\bдо начала\b", "после начала"),
            (r"\bвне\b", "в пределах"),
            (r"\bпо возможности\b", "в обязательном порядке"),
            (r"\bлюбой\b", "только старший"),
            (r"\bвсе\b", "только отдельные"),
        ],
        [
            (r"\b(\d+)\b", lambda m: str(int(m.group(1)) + 5)),
            (r"\bличного состава\b", "командного состава"),
            (r"\bкомандного состава\b", "вспомогательного персонала"),
            (r"\bвоенной полиции\b", "медицинского персонала"),
            (r"\bправоохранител", "инженер"),
            (r"\bподозреваем", "свидетел"),
            (r"\bоружи", "медицинское снаряжени"),
            (r"\bНе ([А-Яа-яЁё-]+йте)\b", lambda m: m.group(1)),
            (r"\bне ([А-Яа-яЁё-]+ть)\b", lambda m: m.group(1)),
        ],
    ]

    for replacements in replacement_sets:
        changed = replace_once(correct, replacements)
        if changed and changed != correct and changed not in variants:
            variants.append(changed)

    fallbacks = [
        "Только командный состав вправе применять следующее положение: " + correct[0].lower() + correct[1:],
        "Это положение применяется лишь после отдельного разрешения командования: " + correct[0].lower() + correct[1:],
        "В зоне операции действует обратное правило: " + correct[0].lower() + correct[1:],
    ]
    for fallback in fallbacks:
        if fallback not in variants:
            variants.append(fallback)
        if len(variants) == 3:
            break

    return variants[:3]


def yaml_string(value: str) -> str:
    return json.dumps(value, ensure_ascii=False)


def question_block(
    question_id: str,
    prompt: str,
    fact: Fact,
    pool: str,
    ordinal: int,
) -> str:
    answers = [fact.text, *make_distractors(fact.text)]
    correct_index = ordinal % 4
    correct = answers.pop(0)
    answers.insert(correct_index, correct)

    lines = [
        "- type: roleTestQuestion",
        f"  id: {question_id}",
        f"  text: {yaml_string(prompt)}",
        "  answers:",
        *[f"    - {yaml_string(answer)}" for answer in answers],
        f"  correctAnswer: {correct_index}",
        f"  pools: [ {yaml_string(pool)} ]",
        f"  source: {yaml_string(f'{fact.source}#{fact.heading}')}",
        "",
    ]
    return "\n".join(lines)


def select_facts(facts: list[Fact], count: int, offset: int = 0) -> list[Fact]:
    if len(facts) < count:
        raise RuntimeError(f"Недостаточно исходных положений: найдено {len(facts)}, требуется {count}")

    rotated = facts[offset % len(facts):] + facts[:offset % len(facts)]
    return [
        rotated[min(len(rotated) - 1, index * len(rotated) // count)]
        for index in range(count)
    ]


def main() -> None:
    blocks = [
        "# Generated from in-game CMU/AU14 rules, UCMJ, SOP and guides.",
        "# Run: python Tools/role_tests/generate_questions.py",
        "",
    ]

    for role_id in ROLE_LABELS:
        blocks.extend([
            "- type: roleTestQuestionPool",
            f"  id: {role_id}",
            f"  job: {role_id}",
            f"  pool: {yaml_string(f'job:{role_id}')}",
            "",
        ])

    common_facts = select_facts(extract_facts(COMMON_SOURCES), COMMON_COUNT)
    for index, fact in enumerate(common_facts, 1):
        blocks.append(question_block(
            f"RoleTestCommon{index:03}",
            f"Какое положение прямо указано в разделе «{fact.heading}» внутриигровых правил?",
            fact,
            "common",
            index,
        ))

    law_facts = select_facts(extract_facts(LAW_SOURCES), LAW_COUNT)
    for index, fact in enumerate(law_facts, 1):
        blocks.append(question_block(
            f"RoleTestLaw{index:03}",
            f"Какое положение прямо установлено разделом «{fact.heading}» Единого кодекса военной юстиции?",
            fact,
            "law",
            index,
        ))

    role_descriptions = load_role_description_facts()
    for role_index, (role_id, label) in enumerate(ROLE_LABELS.items()):
        group = ROLE_TO_GROUP[role_id]
        facts = extract_facts(ROLE_GROUPS[group])
        if role_id in role_descriptions:
            facts = [role_descriptions[role_id], *facts]
        facts = facts[:ROLE_COUNT]
        if len(facts) < ROLE_COUNT:
            raise RuntimeError(
                f"Для {role_id} найдено только {len(facts)} профильных положений из {ROLE_COUNT}"
            )
        for index, fact in enumerate(facts, 1):
            blocks.append(question_block(
                f"RoleTest{role_id}{index:03}",
                f"Какое положение из внутриигрового руководства должен учитывать персонаж на роли «{label}»?",
                fact,
                f"job:{role_id}",
                index + role_index,
            ))

    OUTPUT.write_text("\n".join(blocks), encoding="utf-8", newline="\n")
    print(
        f"Generated {COMMON_COUNT} common, {LAW_COUNT} law and "
        f"{len(ROLE_LABELS) * ROLE_COUNT} role questions ({len(ROLE_LABELS)} role pools)."
    )


if __name__ == "__main__":
    main()
