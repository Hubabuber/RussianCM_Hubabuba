import argparse
import json
import re
import subprocess
import time
import urllib.parse
import urllib.request
from dataclasses import dataclass
from os import replace
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
PROTO_DIR = ROOT / "Resources" / "Prototypes"
EN_DIR = ROOT / "Resources" / "Locale" / "en-US"
RU_DIR = ROOT / "Resources" / "Locale" / "ru-RU"
CACHE_PATH = ROOT / "Tools" / "localization" / ".translation-cache.json"
HEAD_PROTO_CACHE: dict[Path, dict[tuple[str, str], dict[str, str]]] = {}

CYRILLIC_RE = re.compile(r"[А-Яа-яЁё]")
LATIN_RE = re.compile(r"[A-Za-z]")
MESSAGE_RE = re.compile(r"^([A-Za-z0-9_.:-]+)\s*=\s*(.*)$")
ATTR_RE = re.compile(r"^\s+\.([A-Za-z0-9_.:-]+)\s*=\s*(.*)$")
LOC_ID_RE = re.compile(r"^[A-Za-z0-9_.:-]+$")
MOJIBAKE_RE = re.compile(r"[РЎРљРџР°РµС][\x80-\xBFРЂ-уї]")

TOP_LEVEL_ITEM_RE = re.compile(r"^\s{0,1}-\s+type:\s*(?P<type>[A-Za-z0-9_.:-]+)\s*(?:#.*)?$")
TOP_LEVEL_ID_RE = re.compile(r"^\s{2,3}id:\s*(?P<id>[A-Za-z0-9_.:=+*-]+)(?:\s+#.*)?$")
TOP_LEVEL_SCALAR_RE = re.compile(
    r"^(?P<indent>\s{2,3})(?P<key>[A-Za-z0-9_.:-]+)\s*:\s*(?P<value>.+?)(?P<comment>\s+#.*)?$"
)
ANCHOR_VALUE_RE = re.compile(r"&(?P<anchor>[A-Za-z0-9_-]+)\s+(?P<value>[A-Za-z0-9_.:=+-]+)")

LOCALIZED_PREFIXES = (
    "ent-",
    "entity-",
    "reagent-",
    "tile-",
    "alert-",
    "action-",
    "job-",
    "department-",
    "role-",
    "trait-",
    "loadout-",
    "loadoutgroup-",
    "id-card-",
    "access-",
    "accesslevel-",
    "accessgroup-",
    "barsign-",
    "cm-",
    "rmc-",
    "au14-",
    "cmu-",
    "humanoid-",
    "species-",
    "marking-",
    "markings-",
    "objective-",
    "store-",
    "research-",
    "technology-",
    "lathe-",
    "lathecategory-",
    "gas-",
    "material-",
    "stack-",
    "tool-quality-",
    "construction-",
    "salvage-",
    "radio-",
    "radiochannel-",
    "speech-",
    "ghost-role-",
    "guide-entry-",
    "guideentry-",
    "game-preset-",
    "gamepreset-",
    "customholiday-",
    "announcement-",
    "announcementpreset-",
    "alerts-",
    "platoon-",
    "lore-primer-",
    "loreprimer-",
    "rank-",
    "vendor-",
    "requisition-",
    "bounty-",
    "cargobounty-",
    "cargo-",
    "cargoaccount-",
    "verb-",
    "ui-",
    "cmd-",
    "admin-",
    "guidebook-",
    "rumc-",
    "storecategory-",
    "roletype-",
    "techdiscipline-",
    "traitcategory-",
    "authirdparty-",
    "rmcconstruction-",
    "microwavemealrecipe-",
)

REFERENCE_SUFFIX_TRANSLATIONS = {
    "-суффикс": "-suffix",
    "-дес": "-desc",
    "-десс": "-desc",
    "-деска": "-desc",
    "-описание": "-desc",
    "-имя": "-name",
    "-название": "-name",
}
REFERENCE_SUFFIXES = tuple(REFERENCE_SUFFIX_TRANSLATIONS)

TECHNICAL_SCALAR_FIELDS = {
    "id",
    "type",
    "parent",
    "abstract",
    "sprite",
    "icon",
    "color",
    "baseTurf",
    "itemDrop",
    "graph",
    "startNode",
    "targetNode",
    "objectType",
    "placementMode",
    "result",
    "product",
    "group",
    "category",
    "faction",
    "playTimeTracker",
    "startingGear",
    "dummyStartingGear",
    "useLoadoutOfJob",
    "whitelistParent",
    "jobPreviewEntity",
    "entityPrototype",
    "prototype",
    "path",
    "mapPath",
    "atlas",
    "speaker",
    "sex",
    "bodyPart",
    "markingCategory",
    "paygrade",
    "rank",
    "prefix",
    "malePrefix",
    "femalePrefix",
}

# Top-level fields where the consuming code already calls Loc.GetString or
# where this script/code pass intentionally converts the consumer to Loc.GetString.
LOCALIZABLE_FIELDS_BY_TYPE: dict[str, set[str]] = {
    "alert": {"name", "description"},
    "tile": {"name", "suffix"},
    "job": {
        "name",
        "description",
        "supervisors",
        "spawnMenuRoleName",
        "overwatchRoleName",
        "newToJobInfo",
    },
    "department": {"name", "description", "customName"},
    "accessLevel": {"name"},
    "accessGroup": {"name"},
    "reagent": {"name", "desc", "physicalDesc"},
    "material": {"name"},
    "stack": {"name"},
    "guideEntry": {"name"},
    "technology": {"name"},
    "techDiscipline": {"name"},
    "trait": {"name", "description"},
    "traitCategory": {"name"},
    "roleType": {"name"},
    "loadoutGroup": {"name"},
    "radioChannel": {"name"},
    "cargoAccount": {"name", "code"},
    "cargoBounty": {"name", "description"},
    "latheCategory": {"name"},
    "storeCategory": {"name"},
    "gamePreset": {"name", "description"},
    "customHoliday": {"name", "description"},
    "announcementPreset": {"name", "description"},
    "platoon": {"name", "lorePrimer"},
    "lorePrimer": {"planetText", "PlatoonInfo", "threattext"},
    "auThirdParty": {"displayName"},
    "rmcConstruction": {"name"},
    "construction": {"name", "description"},
    "microwaveMealRecipe": {"name"},
}

GLOSSARY = {
    "Yautja": "яутжа",
    "Predalien": "предалиен",
    "Xenomorph": "ксеноморф",
    "xenomorph": "ксеноморф",
    "Xenomorphs": "ксеноморфы",
    "xenomorphs": "ксеноморфы",
    "Xeno": "ксено",
    "xeno": "ксено",
    "Xenos": "ксено",
    "xenos": "ксено",
    "Xenonid": "ксенонид",
    "xenonid": "ксенонид",
    "Weyland-Yutani": "Вейланд-Ютани",
    "Weyland Yutani": "Вейланд-Ютани",
    "We-Ya": "We-Ya",
    "WeYa": "WeYa",
    "USCM": "ККМ США",
    "UNMC": "ККМ ООН",
    "RMC": "RMC",
    "UPP": "UPP",
    "CLF": "CLF",
    "CMB": "CMB",
    "PMC": "PMC",
    "Marine": "морпех",
    "marine": "морпех",
    "Marines": "морпехи",
    "marines": "морпехи",
    "synth": "синтетик",
    "Synth": "синтетик",
    "Synthetic": "синтетик",
    "synthetic": "синтетик",
    "dropship": "десантный корабль",
    "Dropship": "десантный корабль",
    "squad": "отделение",
    "Squad": "отделение",
    "platoon": "взвод",
    "Platoon": "взвод",
    "fireteam": "огневая группа",
    "Fireteam": "огневая группа",
    "corpsman": "санитар",
    "Corpsman": "санитар",
    "rifleman": "стрелок",
    "Rifleman": "стрелок",
    "ram": "таран",
    "Ram": "таран",
}


class IgnoreUnknown(yaml.SafeLoader):
    pass


def ignore_unknown_tag(loader, _tag_suffix, node):
    if isinstance(node, yaml.ScalarNode):
        return loader.construct_scalar(node)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node)
    return None


IgnoreUnknown.add_multi_constructor("!", ignore_unknown_tag)


@dataclass
class PrototypeBlock:
    proto_type: str
    proto_id: str
    path: Path
    fields: dict[str, str]


def has_cyrillic(text: str) -> bool:
    return bool(CYRILLIC_RE.search(text))


def has_latin(text: str) -> bool:
    return bool(LATIN_RE.search(text))


def sanitize(value: str) -> str:
    value = value.replace("\r\n", " ").replace("\n", " ")
    return re.sub(r"\s+", " ", value).strip()


def strip_quotes(value: str) -> tuple[str, str]:
    text = value.strip()
    if len(text) >= 2 and text[0] == text[-1] and text[0] in ("'", '"'):
        return text[1:-1], text[0]
    return text, ""


def is_probable_loc_id(value: str) -> bool:
    stripped = value.strip().strip("'\"")
    if not LOC_ID_RE.match(stripped):
        return False
    return stripped.startswith(LOCALIZED_PREFIXES)


def normalize_reference_literal(value: str | None) -> str | None:
    if not value:
        return None

    stripped = sanitize(value).strip("'\"")
    if not stripped or " " in stripped or "{" in stripped or "}" in stripped:
        return None

    for source, target in REFERENCE_SUFFIX_TRANSLATIONS.items():
        if stripped.endswith(source):
            stripped = stripped[: -len(source)] + target
            break

    if stripped.lower().startswith(LOCALIZED_PREFIXES):
        return stripped
    return None


def loc_key_candidates(reference: str) -> list[str]:
    candidates: list[str] = []

    def add(candidate: str) -> None:
        if candidate not in candidates:
            candidates.append(candidate)

    base_candidates: list[str] = []

    def add_base(candidate: str) -> None:
        if candidate not in base_candidates:
            base_candidates.append(candidate)

    add_base(reference)
    if reference.lower() != reference:
        add_base(reference.lower())
    for candidate in list(base_candidates):
        if candidate.startswith("entity-"):
            add_base(f"ent-{candidate[7:]}")
        if candidate.startswith("rumc-guide-entry-"):
            add_base(f"rmc-guide-entry-{candidate[17:]}")

    for candidate in base_candidates:
        add(candidate)
        if candidate.endswith("-desc"):
            add(f"{candidate[:-5]}.desc")
            add(candidate[:-5])
        if candidate.endswith("-suffix"):
            add(f"{candidate[:-7]}.suffix")
            add(candidate[:-7])
        if candidate.endswith("-name"):
            add(candidate[:-5])
    return candidates


def resolve_locale_reference(value: str | None, values: dict[str, str], seen: set[str] | None = None) -> str | None:
    reference = normalize_reference_literal(value)
    if reference is None:
        return None

    seen = seen or set()
    for candidate in loc_key_candidates(reference):
        if candidate in seen:
            continue
        target = values.get(candidate)
        if not target:
            continue

        nested = resolve_locale_reference(target, values, seen | {candidate})
        if nested is not None:
            return nested
        if normalize_reference_literal(target) is None:
            return target

    return None


def looks_like_reference_literal(value: str | None) -> bool:
    if not value:
        return False
    if normalize_reference_literal(value) is not None:
        return True
    stripped = sanitize(value).strip("'\"")
    lowered = stripped.lower()
    return lowered.startswith(LOCALIZED_PREFIXES) or stripped.endswith(REFERENCE_SUFFIXES)


def resolve_value_for_key(key: str, values: dict[str, str]) -> str | None:
    for candidate in loc_key_candidates(key):
        if candidate == key:
            continue
        target = values.get(candidate)
        if not target:
            continue

        resolved = resolve_locale_reference(target, values)
        if resolved is not None:
            return resolved
        if not looks_like_reference_literal(target):
            return target
    return None


def humanize_identifier(identifier: str) -> str:
    text = re.sub(r"^(?:CMU|RMC|CM|AU14)", "", identifier)
    text = text.replace("_", " ").replace("-", " ")
    text = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", text)
    text = re.sub(r"(?<=[A-Za-z])(?=\d)", " ", text)
    text = re.sub(r"(?<=\d)(?=[A-Za-z])", " ", text)
    return sanitize(text)


def derive_reference_literal(value: str | None, key: str, cache: dict[str, str], use_network: bool, russian: bool) -> str | None:
    if not (key.endswith(".suffix") or key.endswith("-suffix")):
        return None

    reference = normalize_reference_literal(value)
    if reference is None or not reference.endswith("-suffix"):
        return None

    base = reference[:-7]
    if base.startswith("ent-"):
        base = base[4:]

    number = re.search(r"(\d+)$", base)
    if number:
        return number.group(1)

    lower = base.lower()
    direct_ru = {
        "filled": "Заполнено",
        "flipped": "Перевёрнуто",
        "inverted": "Инвертировано",
        "tail": "Хвост",
        "wingport": "Левое крыло",
        "wingstarboard": "Правое крыло",
        "distresssignal": "Сигнал бедствия",
        "fog": "Туман",
    }
    direct_en = {
        "filled": "Filled",
        "flipped": "Flipped",
        "inverted": "Inverted",
        "tail": "Tail",
        "wingport": "Port wing",
        "wingstarboard": "Starboard wing",
        "distresssignal": "Distress signal",
        "fog": "Fog",
    }
    for needle, ru_text in direct_ru.items():
        if needle in lower:
            return ru_text if russian else direct_en[needle]

    english = humanize_identifier(base)
    if not english:
        return None
    return google_translate(english, cache, False) if russian else english


def should_translate_text(value: str) -> bool:
    text = value.strip()
    if not text:
        return False
    if has_cyrillic(text):
        return False
    if is_probable_loc_id(text):
        return False
    if text.startswith("/") or text.startswith("#"):
        return False
    if text in ("true", "false", "null", "None"):
        return False
    return has_latin(text)


def fix_mojibake(value: str) -> str:
    if not MOJIBAKE_RE.search(value):
        return value
    try:
        fixed = value.encode("cp1251").decode("utf-8")
    except UnicodeError:
        return value
    return fixed if has_cyrillic(fixed) else value


def apply_glossary(value: str) -> str:
    out = value
    for source, target in sorted(GLOSSARY.items(), key=lambda item: -len(item[0])):
        out = re.sub(rf"\b{re.escape(source)}\b", target, out)
    return out


def postprocess_ru(value: str) -> str:
    value = fix_mojibake(value)
    replacements = {
        "Яутья": "яутжа",
        "Яутджа": "яутжа",
        "ксеноморфыы": "ксеноморфы",
        "Баран": "Таран",
        "баран": "таран",
        "Спаунер": "спаунер",
        "Шаттл": "шаттл",
        "дропшип": "десантный корабль",
        "Дропшип": "Десантный корабль",
        "Вейланд Ютани": "Вейланд-Ютани",
    }
    for source, target in replacements.items():
        value = value.replace(source, target)
    return value


def load_cache() -> dict[str, str]:
    if not CACHE_PATH.exists():
        return {}
    try:
        return json.loads(CACHE_PATH.read_text(encoding="utf-8"))
    except Exception:
        return {}


def save_cache(cache: dict[str, str]) -> None:
    write_text_retry(
        CACHE_PATH,
        json.dumps(cache, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
    )


def write_text_retry(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(f"{path.name}.tmp")
    last_error: OSError | None = None
    for _ in range(5):
        try:
            tmp.write_text(text, encoding="utf-8", newline="\n")
            replace(tmp, path)
            return
        except OSError as exc:
            last_error = exc
            time.sleep(0.2)

    if tmp.exists():
        try:
            tmp.unlink()
        except OSError:
            pass
    if last_error is not None:
        raise last_error


def continuation_end(block: list[str], start: int, base_indent: int) -> int:
    end = start + 1
    while end < len(block):
        line = block[end]
        if not line.strip():
            lookahead = end + 1
            while lookahead < len(block) and not block[lookahead].strip():
                lookahead += 1
            if lookahead >= len(block):
                break
            indent = len(block[lookahead]) - len(block[lookahead].lstrip(" "))
            if indent <= base_indent:
                break
            end = lookahead
            continue
        indent = len(line) - len(line.lstrip(" "))
        if indent <= base_indent:
            break
        end += 1
    return end


def collect_yaml_anchors(lines: list[str]) -> dict[str, str]:
    anchors: dict[str, str] = {}
    for line in lines:
        match = ANCHOR_VALUE_RE.search(line)
        if match:
            anchors[match.group("anchor")] = match.group("value")
    return anchors


def resolve_yaml_id(raw_id: str, anchors: dict[str, str]) -> str | None:
    if raw_id.startswith("*"):
        return anchors.get(raw_id[1:])
    return raw_id


def google_translate(value: str, cache: dict[str, str], use_network: bool) -> str:
    value = sanitize(value)
    if value in cache:
        return cache[value]
    if not use_network:
        translated = apply_glossary(value)
        cache[value] = translated
        return translated

    prepared = apply_glossary(value)
    params = urllib.parse.urlencode(
        {
            "client": "gtx",
            "sl": "en",
            "tl": "ru",
            "dt": "t",
            "q": prepared,
        }
    )
    try:
        with urllib.request.urlopen(f"https://translate.googleapis.com/translate_a/single?{params}", timeout=20) as response:
            data = json.loads(response.read().decode("utf-8"))
        translated = "".join(part[0] for part in data[0] if part[0])
    except Exception:
        translated = prepared

    translated = postprocess_ru(translated)
    cache[value] = translated
    time.sleep(0.03)
    return translated


def read_ftl_index(root: Path) -> tuple[set[str], dict[str, str], dict[str, Path]]:
    keys: set[str] = set()
    values: dict[str, str] = {}
    paths: dict[str, Path] = {}
    for path in root.rglob("*.ftl"):
        current: str | None = None
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            message = MESSAGE_RE.match(line)
            if message:
                current = message.group(1)
                keys.add(current)
                values[current] = message.group(2).strip()
                paths.setdefault(current, path)
                continue
            attr = ATTR_RE.match(line)
            if attr and current:
                attr_key = f"{current}.{attr.group(1)}"
                keys.add(attr_key)
                values[attr_key] = attr.group(2).strip()
                paths.setdefault(attr_key, path)
    return keys, values, paths


def parse_message_blocks(path: Path) -> list[tuple[str, list[str]]]:
    if not path.exists():
        return []
    blocks: list[tuple[str, list[str]]] = []
    current_key: str | None = None
    current_lines: list[str] = []
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        match = MESSAGE_RE.match(line)
        if match:
            if current_key is not None:
                blocks.append((current_key, current_lines))
            current_key = match.group(1)
            current_lines = [line]
            continue
        if current_key is not None and (line.startswith(" ") or line.startswith("\t")):
            current_lines.append(line)
            continue
        if current_key is not None:
            blocks.append((current_key, current_lines))
            current_key = None
            current_lines = []
    if current_key is not None:
        blocks.append((current_key, current_lines))
    return blocks


def write_blocks(path: Path, blocks: list[tuple[str, list[str]]], prefix_lines: list[str] | None = None) -> None:
    lines: list[str] = []
    if prefix_lines:
        lines.extend(prefix_lines)
    for _, block_lines in blocks:
        lines.extend(block_lines)
    path.parent.mkdir(parents=True, exist_ok=True)
    write_text_retry(path, "\n".join(lines).rstrip() + "\n")


def upsert_simple_message(path: Path, key: str, value: str) -> bool:
    value = sanitize(value)
    if path.exists():
        text = path.read_text(encoding="utf-8")
    else:
        text = ""

    lines = text.splitlines()
    changed = False
    for i, line in enumerate(lines):
        match = MESSAGE_RE.match(line)
        if match and match.group(1) == key:
            if line != f"{key} = {value}":
                lines[i] = f"{key} = {value}"
                changed = True
            break
    else:
        if lines and lines[-1].strip():
            lines.append("")
        lines.append(f"{key} = {value}")
        changed = True

    if changed:
        path.parent.mkdir(parents=True, exist_ok=True)
        write_text_retry(path, "\n".join(lines).rstrip() + "\n")
    return changed


def upsert_entity_message(path: Path, key: str, name: str | None, desc: str | None, suffix: str | None) -> bool:
    blocks = parse_message_blocks(path)
    changed = False
    found = False
    new_blocks: list[tuple[str, list[str]]] = []

    for block_key, lines in blocks:
        if block_key != key:
            new_blocks.append((block_key, lines))
            continue

        found = True
        current_lines = list(lines)
        if name is not None and current_lines[0] != f"{key} = {sanitize(name)}":
            current_lines[0] = f"{key} = {sanitize(name)}"
            changed = True

        attr_positions: dict[str, int] = {}
        for i, line in enumerate(current_lines[1:], start=1):
            attr = ATTR_RE.match(line)
            if attr:
                attr_positions[attr.group(1)] = i

        for attr_name, attr_value in (("desc", desc), ("suffix", suffix)):
            if attr_value is None:
                continue
            new_line = f"    .{attr_name} = {sanitize(attr_value)}"
            if attr_name in attr_positions:
                idx = attr_positions[attr_name]
                if current_lines[idx] != new_line:
                    current_lines[idx] = new_line
                    changed = True
            else:
                current_lines.append(new_line)
                changed = True

        new_blocks.append((block_key, current_lines))

    if not found:
        lines = [f"{key} = {sanitize(name or key)}"]
        if desc is not None:
            lines.append(f"    .desc = {sanitize(desc)}")
        if suffix is not None:
            lines.append(f"    .suffix = {sanitize(suffix)}")
        if blocks:
            new_blocks.append(("", [""]))
        new_blocks.append((key, lines))
        changed = True

    if changed:
        write_blocks(path, new_blocks)
    return changed


def locale_path_for(proto_path: Path, root: Path) -> Path:
    return root / proto_path.relative_to(PROTO_DIR).with_suffix(".ftl")


def run_git_show(rel_path: Path) -> str | None:
    rel = rel_path.as_posix()
    try:
        proc = subprocess.run(
            ["git", "show", f"HEAD:{rel}"],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
    except Exception:
        return None
    if proc.returncode != 0:
        return None
    return proc.stdout


def parse_yaml_items(text: str) -> list[dict]:
    try:
        data = yaml.load(text, Loader=IgnoreUnknown)
    except Exception:
        return []
    if isinstance(data, dict):
        return [data]
    if isinstance(data, list):
        return [item for item in data if isinstance(item, dict)]
    return []


def read_head_prototypes(path: Path) -> dict[tuple[str, str], dict[str, str]]:
    if path in HEAD_PROTO_CACHE:
        return HEAD_PROTO_CACHE[path]

    content = run_git_show(path.relative_to(ROOT))
    if content is None:
        HEAD_PROTO_CACHE[path] = {}
        return HEAD_PROTO_CACHE[path]

    result: dict[tuple[str, str], dict[str, str]] = {}
    for item in parse_yaml_items(content):
        proto_type = item.get("type")
        proto_id = item.get("id")
        if not proto_type or not proto_id:
            continue
        fields = {key: value for key, value in item.items() if isinstance(value, str)}
        result[(str(proto_type), str(proto_id))] = fields
    HEAD_PROTO_CACHE[path] = result
    return result


def append_entity_blocks(path: Path, blocks: list[tuple[str, str, str | None, str | None]]) -> bool:
    if not blocks:
        return False
    existing = path.read_text(encoding="utf-8") if path.exists() else ""
    lines = existing.splitlines()
    if lines and lines[-1].strip():
        lines.append("")
    for key, name, desc, suffix in blocks:
        lines.append(f"{key} = {sanitize(name)}")
        if desc is not None:
            lines.append(f"    .desc = {sanitize(desc)}")
        if suffix is not None:
            lines.append(f"    .suffix = {sanitize(suffix)}")
        lines.append("")
    path.parent.mkdir(parents=True, exist_ok=True)
    write_text_retry(path, "\n".join(lines).rstrip() + "\n")
    return True


def append_simple_messages(path: Path, messages: dict[str, str]) -> bool:
    if not messages:
        return False
    existing = path.read_text(encoding="utf-8") if path.exists() else ""
    lines = existing.splitlines()
    if lines and lines[-1].strip():
        lines.append("")
    for key in sorted(messages):
        lines.append(f"{key} = {sanitize(messages[key])}")
    path.parent.mkdir(parents=True, exist_ok=True)
    write_text_retry(path, "\n".join(lines).rstrip() + "\n")
    return True


def parse_current_blocks(path: Path) -> list[PrototypeBlock]:
    result: list[PrototypeBlock] = []
    data = parse_yaml_items(path.read_text(encoding="utf-8-sig"))
    for item in data:
        proto_type = item.get("type")
        proto_id = item.get("id")
        if not proto_type or not proto_id:
            continue
        fields = {key: value for key, value in item.items() if isinstance(value, str)}
        result.append(PrototypeBlock(str(proto_type), str(proto_id), path, fields))
    return result


def first_parent_id(parent) -> str | None:
    if isinstance(parent, list):
        return str(parent[0]) if parent else None
    if parent is None:
        return None
    return str(parent)


def inherited_entity_field(entity_id: str, entities: dict[str, dict], field: str, seen: set[str] | None = None) -> str | None:
    seen = seen or set()
    if entity_id in seen:
        return None
    seen.add(entity_id)

    entity = entities.get(entity_id)
    if not entity:
        return None

    value = entity.get(field)
    if isinstance(value, str) and value.strip():
        return value

    parent = entity.get("parent")
    parents = parent if isinstance(parent, list) else [parent]
    for parent_id_raw in parents:
        if parent_id_raw is None:
            continue
        inherited = inherited_entity_field(str(parent_id_raw), entities, field, seen)
        if inherited is not None:
            return inherited
    return None


def build_entity_maps() -> tuple[dict[str, dict], dict[str, Path]]:
    entities: dict[str, dict] = {}
    paths: dict[str, Path] = {}
    for path in PROTO_DIR.rglob("*"):
        if path.suffix.lower() not in (".yml", ".yaml"):
            continue
        for item in parse_yaml_items(path.read_text(encoding="utf-8-sig")):
            if item.get("type") != "entity" or not item.get("id"):
                continue
            entity_id = str(item["id"])
            entities[entity_id] = item
            paths[entity_id] = path
    return entities, paths


def normalize_generated_key(proto_type: str, proto_id: str, field: str) -> str:
    raw = f"{proto_type}-{proto_id}-{field}"
    raw = raw.replace("_", "-")
    raw = re.sub(r"[^A-Za-z0-9:-]+", "-", raw).strip("-").lower()
    raw = raw.replace("-description", "-desc")
    return raw


def choose_english_value(current: str | None, head: str | None, en_values: dict[str, str] | None = None) -> str | None:
    if en_values:
        for value in (head, current):
            resolved = resolve_locale_reference(value, en_values)
            if resolved is not None:
                return sanitize(resolved)

    if head and should_translate_text(head):
        return sanitize(head)
    if current and not is_probable_loc_id(current) and has_latin(current):
        return sanitize(current)
    value = head or current
    if value and normalize_reference_literal(value) is None:
        return sanitize(value)
    return None


def choose_russian_value(
    english: str | None,
    current: str | None,
    generated_key: str | None,
    ru_values: dict[str, str],
    cache: dict[str, str],
    use_network: bool,
) -> str | None:
    if generated_key:
        for key in (generated_key, generated_key.replace("-description", "-desc")):
            existing = ru_values.get(key)
            resolved = resolve_locale_reference(existing, ru_values)
            if resolved is not None:
                return postprocess_ru(resolved)
            if existing and normalize_reference_literal(existing) is None and has_cyrillic(fix_mojibake(existing)):
                return postprocess_ru(existing)

    resolved_current = resolve_locale_reference(current, ru_values)
    if resolved_current is not None:
        return postprocess_ru(resolved_current)

    if current and normalize_reference_literal(current) is None and has_cyrillic(fix_mojibake(current)):
        return postprocess_ru(current)

    if english is None:
        return None
    return google_translate(english, cache, use_network)


def localize_entities(scope: str, use_network: bool, rewrite_yaml: bool) -> tuple[int, int, int]:
    cache = load_cache()
    ru_keys, ru_values, ru_paths = read_ftl_index(RU_DIR)
    en_keys, en_values, en_paths = read_ftl_index(EN_DIR)
    entities, source_paths = build_entity_maps()

    changed_ftl = 0
    entity_entries = 0
    changed_yaml = 0

    for entity_id in sorted(entities):
        path = source_paths[entity_id]
        if scope and not str(path.relative_to(PROTO_DIR)).startswith(scope):
            continue

        entity = entities[entity_id]
        head_values = read_head_prototypes(path).get(("entity", entity_id), {})
        name_current = inherited_entity_field(entity_id, entities, "name")
        desc_current = inherited_entity_field(entity_id, entities, "description")
        suffix_current = inherited_entity_field(entity_id, entities, "suffix")

        name_en = choose_english_value(name_current, head_values.get("name"), en_values)
        desc_en = choose_english_value(desc_current, head_values.get("description"), en_values)
        suffix_en = choose_english_value(suffix_current, head_values.get("suffix"), en_values)
        fallback_name = False
        if name_en is None and (desc_en is not None or suffix_en is not None):
            name_en = entity_id
            fallback_name = True

        ent_key = f"ent-{entity_id}"
        if not LOC_ID_RE.match(ent_key):
            entity_entries += 1
            continue

        old_name_key = normalize_generated_key("entity", entity_id, "name")
        old_desc_key = normalize_generated_key("entity", entity_id, "description")
        old_suffix_key = normalize_generated_key("entity", entity_id, "suffix")
        name_ru = entity_id if fallback_name else choose_russian_value(name_en, name_current, old_name_key, ru_values, cache, use_network)
        desc_ru = choose_russian_value(desc_en, desc_current, old_desc_key, ru_values, cache, use_network)
        suffix_ru = choose_russian_value(suffix_en, suffix_current, old_suffix_key, ru_values, cache, use_network)

        en_target = en_paths.get(ent_key, locale_path_for(path, EN_DIR))
        ru_target = ru_paths.get(ent_key, locale_path_for(path, RU_DIR))

        en_name_missing = name_en is not None and ent_key not in en_keys
        en_desc_missing = desc_en is not None and f"{ent_key}.desc" not in en_keys
        en_suffix_missing = suffix_en is not None and f"{ent_key}.suffix" not in en_keys
        if en_name_missing or ((en_desc_missing or en_suffix_missing) and ent_key in en_keys):
            changed_ftl += int(upsert_entity_message(
                en_target,
                ent_key,
                name_en if en_name_missing else None,
                desc_en if en_desc_missing else None,
                suffix_en if en_suffix_missing else None,
            ))
            en_keys.add(ent_key)
            if en_desc_missing:
                en_keys.add(f"{ent_key}.desc")
            if en_suffix_missing:
                en_keys.add(f"{ent_key}.suffix")

        ru_name_missing = name_ru is not None and ent_key not in ru_keys
        ru_desc_missing = desc_ru is not None and f"{ent_key}.desc" not in ru_keys
        ru_suffix_missing = suffix_ru is not None and f"{ent_key}.suffix" not in ru_keys
        if ru_name_missing or ((ru_desc_missing or ru_suffix_missing) and ent_key in ru_keys):
            changed_ftl += int(upsert_entity_message(
                ru_target,
                ent_key,
                name_ru if ru_name_missing else None,
                desc_ru if ru_desc_missing else None,
                suffix_ru if ru_suffix_missing else None,
            ))
            ru_keys.add(ent_key)
            if name_ru is not None:
                ru_values[ent_key] = name_ru
            if ru_desc_missing:
                ru_keys.add(f"{ent_key}.desc")
            if ru_suffix_missing:
                ru_keys.add(f"{ent_key}.suffix")
        entity_entries += 1

    save_cache(cache)

    if rewrite_yaml:
        changed_yaml = rewrite_entity_yaml(scope)

    return changed_ftl, entity_entries, changed_yaml


def rewrite_entity_yaml(scope: str) -> int:
    changed = 0
    proto_root = PROTO_DIR / scope if scope else PROTO_DIR
    for path in proto_root.rglob("*"):
        if path.suffix.lower() not in (".yml", ".yaml"):
            continue
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        anchors = collect_yaml_anchors(lines)
        file_changed = False

        def rewrite_block(block: list[str]) -> list[str]:
            nonlocal file_changed
            if not block:
                return block
            type_match = TOP_LEVEL_ITEM_RE.match(block[0])
            if not type_match or type_match.group("type") != "entity":
                return block

            current_id: str | None = None
            for block_line in block:
                id_match = TOP_LEVEL_ID_RE.match(block_line)
                if id_match:
                    current_id = resolve_yaml_id(id_match.group("id"), anchors)
                    break
            if current_id is None:
                return block

            rewritten: list[str] = []
            index = 0
            while index < len(block):
                block_line = block[index]
                scalar = TOP_LEVEL_SCALAR_RE.match(block_line)
                if not scalar or scalar.group("key") not in ("name", "description", "suffix"):
                    rewritten.append(block_line)
                    index += 1
                    continue

                key = scalar.group("key")
                raw_value, quote = strip_quotes(scalar.group("value"))
                desired = {
                    "name": f"ent-{current_id}",
                    "description": f"ent-{current_id}-desc",
                    "suffix": f"ent-{current_id}-suffix",
                }[key]
                if not LOC_ID_RE.match(desired):
                    rewritten.append(block_line)
                    index += 1
                    continue
                end = continuation_end(block, index, len(scalar.group("indent")))
                if raw_value == desired and end == index + 1:
                    rewritten.append(block_line)
                    index += 1
                    continue

                comment = scalar.group("comment") or ""
                quoted = f"{quote}{desired}{quote}" if quote else desired
                rewritten.append(f"{scalar.group('indent')}{key}: {quoted}{comment}")
                file_changed = True
                index = end
            return rewritten

        new_lines: list[str] = []
        current_block: list[str] = []
        for line in lines:
            if TOP_LEVEL_ITEM_RE.match(line):
                new_lines.extend(rewrite_block(current_block))
                current_block = [line]
                continue
            if current_block:
                current_block.append(line)
            else:
                new_lines.append(line)

        new_lines.extend(rewrite_block(current_block))

        if file_changed:
            write_text_retry(path, "\n".join(new_lines) + "\n")
            changed += 1

    return changed


def localize_top_level_fields(scope: str, use_network: bool, rewrite_yaml: bool) -> tuple[int, int, int]:
    cache = load_cache()
    _, ru_values, _ = read_ftl_index(RU_DIR)
    en_keys, en_values, _ = read_ftl_index(EN_DIR)
    ru_keys, _, _ = read_ftl_index(RU_DIR)
    proto_root = PROTO_DIR / scope if scope else PROTO_DIR
    changed_files = 0
    changed_ftl = 0
    fields_seen = 0

    for path in proto_root.rglob("*"):
        if path.suffix.lower() not in (".yml", ".yaml"):
            continue

        head_by_proto = read_head_prototypes(path)
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        anchors = collect_yaml_anchors(lines)
        additions_en: dict[str, str] = {}
        additions_ru: dict[str, str] = {}
        file_changed = False

        def rewrite_block(block: list[str]) -> list[str]:
            nonlocal fields_seen, file_changed
            if not block:
                return block

            type_match = TOP_LEVEL_ITEM_RE.match(block[0])
            if not type_match:
                return block

            current_type = type_match.group("type")
            if current_type == "entity":
                return block

            allowed_fields = LOCALIZABLE_FIELDS_BY_TYPE.get(current_type, set())
            if not allowed_fields:
                return block

            current_id: str | None = None
            for block_line in block:
                id_match = TOP_LEVEL_ID_RE.match(block_line)
                if id_match:
                    current_id = resolve_yaml_id(id_match.group("id"), anchors)
                    break

            if current_id is None:
                return block

            rewritten: list[str] = []
            index = 0
            while index < len(block):
                block_line = block[index]
                scalar = TOP_LEVEL_SCALAR_RE.match(block_line)
                if not scalar:
                    rewritten.append(block_line)
                    index += 1
                    continue

                field = scalar.group("key")
                if field in TECHNICAL_SCALAR_FIELDS or field not in allowed_fields:
                    rewritten.append(block_line)
                    index += 1
                    continue

                current_value, quote = strip_quotes(scalar.group("value"))
                head_value = head_by_proto.get((current_type, current_id), {}).get(field)
                english = choose_english_value(current_value, head_value, en_values)
                if english is None:
                    rewritten.append(block_line)
                    index += 1
                    continue

                loc_key = normalize_generated_key(current_type, current_id, field)
                russian = choose_russian_value(english, current_value, loc_key, ru_values, cache, use_network)
                if russian is None:
                    rewritten.append(block_line)
                    index += 1
                    continue

                if loc_key not in en_keys:
                    additions_en[loc_key] = english
                    en_keys.add(loc_key)
                if loc_key not in ru_keys:
                    additions_ru[loc_key] = russian
                    ru_keys.add(loc_key)
                    ru_values[loc_key] = russian
                fields_seen += 1

                end = continuation_end(block, index, len(scalar.group("indent")))
                if rewrite_yaml and (current_value != loc_key or end != index + 1):
                    comment = scalar.group("comment") or ""
                    quoted = f"{quote}{loc_key}{quote}" if quote else loc_key
                    rewritten.append(f"{scalar.group('indent')}{field}: {quoted}{comment}")
                    file_changed = True
                    index = end
                else:
                    rewritten.append(block_line)
                    index += 1

            return rewritten

        new_lines: list[str] = []
        current_block: list[str] = []
        for line in lines:
            if TOP_LEVEL_ITEM_RE.match(line):
                new_lines.extend(rewrite_block(current_block))
                current_block = [line]
                continue
            if current_block:
                current_block.append(line)
            else:
                new_lines.append(line)

        new_lines.extend(rewrite_block(current_block))

        if additions_en:
            target = locale_path_for(path, EN_DIR)
            changed_ftl += int(append_simple_messages(target, additions_en))
        if additions_ru:
            target = locale_path_for(path, RU_DIR)
            changed_ftl += int(append_simple_messages(target, additions_ru))

        if file_changed:
            write_text_retry(path, "\n".join(new_lines) + "\n")
            changed_files += 1

    save_cache(cache)
    return changed_ftl, fields_seen, changed_files


def resolve_scopes(raw: str) -> list[str]:
    if raw.strip() == "":
        return [""]
    aliases = {"_RMC": "_RMC14", "_RMC14": "_RMC14", "_AU14": "_AU14", "_CMU14": "_CMU14"}
    scopes: list[str] = []
    for part in re.split(r"[,;]\s*|\s+", raw.strip()):
        if not part:
            continue
        scopes.append(aliases.get(part, part))
    return list(dict.fromkeys(scopes))


def cleanup_ftl_reference_literals(root: Path, use_network: bool = False) -> tuple[int, int]:
    _, values, _ = read_ftl_index(root)
    fallback_values: dict[str, str] = {}
    cache = load_cache() if root == RU_DIR else {}
    if root == RU_DIR:
        _, fallback_values, _ = read_ftl_index(EN_DIR)

    changed_files = 0
    replacements = 0

    for path in root.rglob("*.ftl"):
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        current: str | None = None
        file_changed = False

        for i, line in enumerate(lines):
            message = MESSAGE_RE.match(line)
            if message:
                current = message.group(1)
                key = current
                value = message.group(2).strip()
            else:
                attr = ATTR_RE.match(line)
                if not attr or current is None:
                    continue
                key = f"{current}.{attr.group(1)}"
                value = attr.group(2).strip()

            if not looks_like_reference_literal(value):
                continue

            resolved = resolve_locale_reference(value, values)
            if resolved is None:
                resolved = resolve_value_for_key(key, values)
            if resolved is None and fallback_values:
                english = fallback_values.get(key)
                if english is None:
                    english = resolve_value_for_key(key, fallback_values)
                elif looks_like_reference_literal(english):
                    resolved = resolve_locale_reference(english, values)
                    english = resolve_locale_reference(english, fallback_values)

                if resolved is None and english is not None and not looks_like_reference_literal(english):
                    resolved = google_translate(english, cache, use_network)
            if resolved is None:
                resolved = derive_reference_literal(value, key, cache, use_network, root == RU_DIR)

            if resolved is None:
                continue

            resolved = postprocess_ru(resolved) if root == RU_DIR else sanitize(resolved)
            if sanitize(resolved) == sanitize(value):
                continue

            prefix = line[: line.index("=") + 1]
            lines[i] = f"{prefix} {sanitize(resolved)}"
            values[key] = sanitize(resolved)
            file_changed = True
            replacements += 1

        if file_changed:
            write_text_retry(path, "\n".join(lines).rstrip() + "\n")
            changed_files += 1

    if root == RU_DIR:
        save_cache(cache)

    return replacements, changed_files


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--scope", default="", help="Prototype scope under Resources/Prototypes. Empty means whole build.")
    parser.add_argument("--no-network", action="store_true")
    parser.add_argument("--no-rewrite-yaml", action="store_true")
    parser.add_argument("--skip-entities", action="store_true")
    parser.add_argument("--skip-fields", action="store_true")
    args = parser.parse_args()

    total_entity_ftl = 0
    total_entity_entries = 0
    total_entity_yaml = 0
    total_field_ftl = 0
    total_fields = 0
    total_field_yaml = 0

    for scope in resolve_scopes(args.scope):
        if not args.skip_entities:
            ftl, entries, yaml_files = localize_entities(scope, not args.no_network, not args.no_rewrite_yaml)
            total_entity_ftl += ftl
            total_entity_entries += entries
            total_entity_yaml += yaml_files
        if not args.skip_fields:
            ftl, fields, yaml_files = localize_top_level_fields(scope, not args.no_network, not args.no_rewrite_yaml)
            total_field_ftl += ftl
            total_fields += fields
            total_field_yaml += yaml_files

    print(f"Entity FTL writes: {total_entity_ftl}")
    print(f"Entity messages touched: {total_entity_entries}")
    print(f"Entity YAML files rewritten: {total_entity_yaml}")
    print(f"Field FTL writes: {total_field_ftl}")
    print(f"Fields covered: {total_fields}")
    print(f"Field YAML files rewritten: {total_field_yaml}")

    en_resolved, en_files = cleanup_ftl_reference_literals(EN_DIR)
    ru_resolved, ru_files = cleanup_ftl_reference_literals(RU_DIR, not args.no_network)
    print(f"EN reference literals resolved: {en_resolved} in {en_files} files")
    print(f"RU reference literals resolved: {ru_resolved} in {ru_files} files")


if __name__ == "__main__":
    main()
