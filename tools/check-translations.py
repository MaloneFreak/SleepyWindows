"""Checks every lang/*.txt against lang/en.txt.

Fails (exit code 1) when a translation has an unknown key, a duplicated key,
a malformed line, or placeholders ({0}, {1}) that don't match English.
Missing keys are only reported as warnings, since the app falls back to English.

Usage: python tools/check-translations.py
"""

import pathlib
import re
import sys

LANG_DIR = pathlib.Path(__file__).resolve().parent.parent / "lang"
PLACEHOLDER = re.compile(r"\{(\d+)(?::[^}]*)?\}")


def parse(path):
    table, errors = {}, []
    for number, raw in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in line or line.index("=") == 0:
            errors.append(f"{path.name}:{number}: expected 'key = value'")
            continue
        key, value = (part.strip() for part in line.split("=", 1))
        if key in table:
            errors.append(f"{path.name}:{number}: duplicate key '{key}'")
        table[key] = value
    return table, errors


def placeholders(value):
    return sorted(set(PLACEHOLDER.findall(value)))


def main():
    english, errors = parse(LANG_DIR / "en.txt")
    warnings = []

    for path in sorted(LANG_DIR.glob("*.txt")):
        if path.name == "en.txt":
            continue
        table, file_errors = parse(path)
        errors += file_errors

        if not table.get("_name"):
            errors.append(f"{path.name}: missing _name (the language's own name)")

        for key, value in table.items():
            if key not in english:
                errors.append(f"{path.name}: unknown key '{key}'")
            elif placeholders(value) != placeholders(english[key]):
                errors.append(
                    f"{path.name}: '{key}' uses {placeholders(value) or 'no'} placeholders, "
                    f"English uses {placeholders(english[key]) or 'none'}"
                )

        for key in english:
            if key not in table:
                warnings.append(f"{path.name}: missing '{key}' (English will be shown)")

    for message in warnings:
        print("warning:", message)
    for message in errors:
        print("error:", message)

    count = len(list(LANG_DIR.glob("*.txt")))
    print(f"{count} language files checked, {len(errors)} error(s), {len(warnings)} warning(s).")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
