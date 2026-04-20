"""
Reads WeaponStats.csv and patches every .asset file in this folder
to use the new flat baseStats / currentStats structure.

Replacement region:
  START — first occurrence of "  magazineSize:" (old format)
           or "  baseStats:"                   (already-patched format)
  END   — "  fireSound:" (footer preserved as-is)

Only Common / Upgrade Level 0 values from the CSV are used as base stats.
magazineSize, startingReservedAmmo and reloadTime are read from the existing
asset and written unchanged into both structs.
currentStats is set to the same values as baseStats.

Run from the Weapons folder:
    python patch_weapons.py
"""

import csv
import os
import re

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CSV_PATH = os.path.join(SCRIPT_DIR, "WeaponStats.csv")

WEAPON_MAP = {
    "AR 1 Modded M4.asset":                          "M4",
    "AR 2 Modded M4-C.asset":                        "M4-C",
    "AR 3 Modded AUG.asset":                         "AUG",
    "AR 4 Modded 552 Commando.asset":                "552 Commando",
    "AR 5 Modded ASH-12.asset":                      "ASH-12",
    "Pistol 1 Modded Hudson H9.asset":               "Hudson H9",
    "Pistol 2 Modded FiveSeven.asset":               "FiveSeven",
    "Pistol 3 Modded Ruger Mark IV 2245 Lite.asset": "Ruger Mark IV",
    "Pistol 4 Modded MicroUzi.asset":                "MicroUzi",
    "Recon 1 Modded CZ 600 Trail.asset":             "CZ 600 Trail",
    "SMG 1 Modded UZI.asset":                        "Uzi",
    "SMG 2 Modded Scorpion EVO.asset":               "Scorpion EVO",
    "SMG 3 Modded P90.asset":                        "P90",
}

AKIMBO_WEAPONS = {"Hudson H9", "FiveSeven", "Ruger Mark IV", "MicroUzi"}


def load_csv():
    """Returns dict: data[gun_name][rarity][upgrade_level] = row_dict"""
    data = {}
    with open(CSV_PATH, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            name   = row["Gun Name"].strip()
            rarity = row["Rarity"].strip()
            level  = int(row["Upgrade Level"].strip())
            data.setdefault(name, {}).setdefault(rarity, {})[level] = row
    return data


def get_field(text, field, default="0"):
    """Extract a scalar field value from a YAML snippet."""
    m = re.search(rf"^\s+{re.escape(field)}: (.+)$", text, re.MULTILINE)
    return m.group(1).strip() if m else default


def get_attachments_from_tiers(text):
    """
    Derive canHaveSight/canHaveLaser/canHaveGrip from old per-rarity fields.
      canHaveSight = hasSight on the Rare tier
      canHaveLaser = hasLaser on the Epic tier
      canHaveGrip  = hasGrip  on the Legendary tier
    """
    att = {}
    for block in re.split(r"(?=  - rarityName:)", text):
        nm = re.search(r"rarityName: (.+)", block)
        if not nm:
            continue
        rarity = nm.group(1).strip()
        att[rarity] = {
            "hasSight": int((re.search(r"hasSight: (\d)", block) or re.search(r"(0)", "0")).group(1)),
            "hasLaser": int((re.search(r"hasLaser: (\d)", block) or re.search(r"(0)", "0")).group(1)),
            "hasGrip":  int((re.search(r"hasGrip: (\d)",  block) or re.search(r"(0)", "0")).group(1)),
        }
    return (
        att.get("Rare",      {}).get("hasSight", 0),
        att.get("Epic",      {}).get("hasLaser", 0),
        att.get("Legendary", {}).get("hasGrip",  0),
    )


def format_float(value):
    """Format a float, stripping trailing zeros but keeping precision."""
    f = float(value)
    s = f"{f:.10f}".rstrip("0").rstrip(".")
    return s if s else "0"


def build_stats_block(label, row, mag, reserve, reload_time):
    """Build a baseStats or currentStats YAML block (4-space indent for struct fields)."""
    lines = [f"  {label}:"]
    lines.append(f"    damage: {int(row['Damage'])}")
    lines.append(f"    headshotDamage: {int(row['HS Damage'])}")
    lines.append(f"    magazineSize: {mag}")
    lines.append(f"    startingReservedAmmo: {reserve}")
    lines.append(f"    reloadTime: {reload_time}")
    lines.append(f"    spreadIntensity: {format_float(row['Spread Intensity'])}")
    lines.append(f"    spreadPerShot: {format_float(row['Spread Per Shot'])}")
    lines.append(f"    spreadRecovery: {format_float(row['Spread Recovery'])}")
    lines.append(f"    shootingDelay: {format_float(row['Shooting Delay'])}")
    lines.append(f"    snappiness: {format_float(row['Snappiness'])}")
    lines.append(f"    returnSpeed: {format_float(row['Return Speed'])}")
    return "\n".join(lines) + "\n"


def patch_asset(asset_path, weapon_name, csv_data):
    with open(asset_path, "r", encoding="utf-8") as f:
        text = f.read()

    # Find replacement region start — works for old and already-patched formats
    start = -1
    for marker in ["  magazineSize:", "  baseStats:"]:
        start = text.find(marker)
        if start != -1:
            break

    footer_start = text.find("  fireSound:")
    if start == -1 or footer_start == -1:
        print(f"  SKIPPED (unexpected format): {os.path.basename(asset_path)}")
        return

    header = text[:start]
    middle = text[start:footer_start]
    footer = text[footer_start:]

    # Extract values to preserve from the region being replaced
    mag         = get_field(middle, "magazineSize",          "30")
    reserve     = get_field(middle, "startingReservedAmmo",  "90")
    reload_time = get_field(middle, "reloadTime",            "1.5")
    can_scope   = get_field(middle, "canScope",              "0")
    zoom_factor = get_field(middle, "zoomFactor",            "2")

    # Attachment capability: read from old rarity tiers or new flat fields
    if "rarityTiers:" in middle:
        can_sight, can_laser, can_grip = get_attachments_from_tiers(middle)
    else:
        can_sight = int(get_field(middle, "canHaveSight", "0"))
        can_laser = int(get_field(middle, "canHaveLaser", "0"))
        can_grip  = int(get_field(middle, "canHaveGrip",  "0"))

    can_akimbo = 1 if weapon_name in AKIMBO_WEAPONS else 0

    # CSV: Common / Upgrade Level 0 as base
    row = csv_data.get(weapon_name, {}).get("Common", {}).get(0)
    if not row:
        print(f"  WARNING: no Common/0 data for {weapon_name}, skipping")
        return

    # Build replacement block
    new_middle  = build_stats_block("baseStats",    row, mag, reserve, reload_time)
    new_middle += build_stats_block("currentStats", row, mag, reserve, reload_time)
    new_middle += f"  canHaveSight: {can_sight}\n"
    new_middle += f"  canHaveLaser: {can_laser}\n"
    new_middle += f"  canHaveGrip: {can_grip}\n"
    new_middle += f"  canBeAkimbo: {can_akimbo}\n"
    new_middle += f"  canScope: {can_scope}\n"
    new_middle += f"  zoomFactor: {zoom_factor}\n"

    with open(asset_path, "w", encoding="utf-8") as f:
        f.write(header + new_middle + footer)

    print(f"  Patched: {os.path.basename(asset_path)}")


def main():
    csv_data = load_csv()
    print(f"Loaded CSV with {sum(len(r) for w in csv_data.values() for r in w.values())} entries\n")

    for filename, weapon_name in WEAPON_MAP.items():
        asset_path = os.path.join(SCRIPT_DIR, filename)
        if not os.path.exists(asset_path):
            print(f"MISSING: {filename}")
            continue
        print(f"Processing {filename} ({weapon_name})...")
        patch_asset(asset_path, weapon_name, csv_data)

    print("\nDone. Reload Unity to see changes.")


if __name__ == "__main__":
    main()
