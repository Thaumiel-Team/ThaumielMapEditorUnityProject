"""
Usage:
    python CleanupAssets.py /path/to/UnityProject
    python CleanupAssets.py --csv unused_assets.csv
    python CleanupAssets.py --min-size 1024 --exclude-scripts
    python CleanupAssets.py --json unused_assets.json
"""

import argparse
import csv
import json
import re
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Set

GUID_RE = re.compile(rb"guid:\s*([0-9a-fA-F]{32})")
IDENT_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")

BINARY_EXTS = {".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr", ".bmp", ".gif", ".tiff", ".wav", ".mp3", ".ogg", ".aiff", ".aif", ".mod", ".it", ".s3m", ".xm", ".mp4", ".mov", ".avi", ".webm", ".ttf", ".otf", ".fbx", ".obj", ".blend", ".max", ".dae", ".dll", ".so", ".dylib", ".exe", ".zip", ".mixer"}
SKIP_DIR_NAMES = {"Library", "Temp", "Obj", "Logs", ".git", ".vs", "Packages", "PackageCache", "Build", "Builds"}
MAX_SCAN_BYTES = 15 * 1024 * 1024
DYNAMIC_RISK_HINTS = ("resources")
DEFAULT_PROTECTED_PATHS = ["Assets/Parts"]

@dataclass
class AssetInfo:
    path: Path
    guid: str
    size: int
    ext: str
    is_script: bool = False
    class_name: str = ""


def human_size(num_bytes: float) -> str:
    for unit in ("B", "KB", "MB", "GB", "TB"):
        if num_bytes < 1024 or unit == "TB":
            return f"{int(num_bytes)} {unit}" if unit == "B" else f"{num_bytes:.1f} {unit}"
        num_bytes /= 1024
    return f"{num_bytes:.1f} TB"


def read_bytes_safely(path: Path, max_bytes: int = MAX_SCAN_BYTES) -> bytes:
    try:
        if path.stat().st_size > max_bytes:
            return b""
        return path.read_bytes()
    except (OSError, PermissionError):
        return b""


def is_dynamic_risk(path: Path) -> bool:
    parts_lower = {p.lower() for p in path.parts}
    return any(hint in parts_lower for hint in DYNAMIC_RISK_HINTS)


def within_skip_dir(rel_path: Path) -> bool:
    return any(part in SKIP_DIR_NAMES for part in rel_path.parts)


def is_protected(path: Path, project_root: Path, protected_paths: List[str]) -> bool:
    rel = path.relative_to(project_root)
    for protected in protected_paths:
        protected_parts = Path(protected).parts
        if rel.parts[:len(protected_parts)] == protected_parts:
            return True
    return False


def collect_assets(project_root: Path, assets_dir: Path) -> Dict[str, AssetInfo]:
    guid_to_asset: Dict[str, AssetInfo] = {}

    for path in assets_dir.rglob("*"):
        if path.is_dir() or path.suffix == ".meta":
            continue
        if within_skip_dir(path.relative_to(project_root)):
            continue

        meta_path = path.with_name(path.name + ".meta")
        if not meta_path.exists():
            continue

        match = GUID_RE.search(read_bytes_safely(meta_path))
        if not match:
            continue
        guid = match.group(1).decode("ascii").lower()

        ext = path.suffix.lower()
        is_script = ext == ".cs"
        guid_to_asset[guid] = AssetInfo(
            path=path,
            guid=guid,
            size=path.stat().st_size,
            ext=ext,
            is_script=is_script,
            class_name=path.stem if is_script else "",
        )

    return guid_to_asset


def scan_guid_references(project_root: Path, assets_dir: Path) -> Dict[str, int]:
    reference_count: Dict[str, int] = defaultdict(int)

    scan_roots = [assets_dir]
    proj_settings = project_root / "ProjectSettings"
    if proj_settings.exists():
        scan_roots.append(proj_settings)

    for root in scan_roots:
        for path in root.rglob("*"):
            if path.is_dir() or path.suffix == ".meta":
                continue
            if path.suffix.lower() in BINARY_EXTS:
                continue
            if within_skip_dir(path.relative_to(project_root)):
                continue

            data = read_bytes_safely(path)
            if not data:
                continue
            for match in GUID_RE.finditer(data):
                guid = match.group(1).decode("ascii").lower()
                reference_count[guid] += 1

    return reference_count


def scan_script_token_usage(guid_to_asset: Dict[str, AssetInfo]) -> Set[str]:
    scripts = [a for a in guid_to_asset.values() if a.is_script]
    tokens_by_guid: Dict[str, Set[str]] = {}

    for asset in scripts:
        text = read_bytes_safely(asset.path).decode("utf-8", errors="ignore")
        tokens_by_guid[asset.guid] = set(IDENT_RE.findall(text))

    used_guids: Set[str] = set()
    for target in scripts:
        for other_guid, tokens in tokens_by_guid.items():
            if other_guid == target.guid:
                continue
            if target.class_name and target.class_name in tokens:
                used_guids.add(target.guid)
                break

    return used_guids


def find_build_scene_guids(project_root: Path) -> Set[str]:
    ebs = project_root / "ProjectSettings" / "EditorBuildSettings.asset"
    if not ebs.exists():
        return set()
    data = read_bytes_safely(ebs)
    return {m.group(1).decode("ascii").lower() for m in GUID_RE.finditer(data)}


def analyze(project_root: Path, min_size: int, exclude_scripts: bool,
            protected_paths: List[str]):
    assets_dir = project_root / "Assets"
    if not assets_dir.exists():
        sys.exit(f"Error: {assets_dir} not found. Point this at a Unity project root.")

    print("Scanning assets...")
    guid_to_asset = collect_assets(project_root, assets_dir)
    print(f"  Found {len(guid_to_asset)} tracked assets.")

    print("Scanning for GUID references (scenes, prefabs, materials, etc.)...")
    reference_count = scan_guid_references(project_root, assets_dir)

    print("Scanning scripts for code level usage...")
    code_used_guids = scan_script_token_usage(guid_to_asset)

    build_scene_guids = find_build_scene_guids(project_root)

    unused: List[AssetInfo] = []
    for guid, asset in guid_to_asset.items():
        if exclude_scripts and asset.is_script:
            continue
        if asset.size < min_size:
            continue
        if reference_count.get(guid, 0) > 0:
            continue
        if guid in code_used_guids:
            continue
        if guid in build_scene_guids:
            continue
        if is_protected(asset.path, project_root, protected_paths):
            continue
        unused.append(asset)

    unused.sort(key=lambda a: a.size, reverse=True)
    return unused, guid_to_asset


def print_report(unused: List[AssetInfo], total_assets: int, project_root: Path):
    total_bytes = sum(a.size for a in unused)
    print()
    print("=" * 70)
    print(f"UNUSED ASSETS: {len(unused)} of {total_assets} tracked assets")
    print(f"RECLAIMABLE SPACE: {human_size(total_bytes)}")
    print("=" * 70)

    if not unused:
        print("No unused assets found (or everything was filtered out).")
        return

    for asset in unused:
        rel = asset.path.relative_to(project_root)
        risk = "  [DYNAMIC RISK - check Resources usage]" if is_dynamic_risk(asset.path) else ""
        print(f"  {human_size(asset.size):>10}   {rel}{risk}")


def write_csv(unused: List[AssetInfo], project_root: Path, out_path: Path):
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["path", "size_bytes", "size_human", "extension", "dynamic_risk"])
        for asset in unused:
            rel = asset.path.relative_to(project_root)
            writer.writerow([str(rel), asset.size, human_size(asset.size), asset.ext,
                              is_dynamic_risk(asset.path)])
    print(f"CSV written to {out_path}")


def write_json(unused: List[AssetInfo], project_root: Path, out_path: Path):
    payload = [
        {
            "path": str(a.path.relative_to(project_root)),
            "size_bytes": a.size,
            "size_human": human_size(a.size),
            "extension": a.ext,
            "guid": a.guid,
            "dynamic_risk": is_dynamic_risk(a.path),
        }
        for a in unused
    ]
    out_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"JSON written to {out_path}")


def main():
    parser = argparse.ArgumentParser(description="Find unused assets in a Unity project.")
    parser.add_argument("project", nargs="?", default=".",
                         help="Path to the Unity project root (the folder containing Assets/).")
    parser.add_argument("--min-size", type=int, default=0,
                         help="Ignore assets smaller than this many bytes.")
    parser.add_argument("--exclude-scripts", action="store_true",
                         help="Skip .cs scripts entirely (fewer false positives).")
    parser.add_argument("--csv", type=str, default=None, help="Write results to this CSV file.")
    parser.add_argument("--json", type=str, default=None, help="Write results to this JSON file.")
    parser.add_argument("--protect", action="append", default=[],
                         help="Additional folder (relative to project root, e.g. Assets/Characters) "
                              "to never flag as unused. Can be passed multiple times. "
                              f"Assets/Parts is protected by default.")
    parser.add_argument("--no-default-protect", action="store_true",
                         help="Don't auto-protect Assets/Parts; only use --protect paths.")
    args = parser.parse_args()

    project_root = Path(args.project).resolve()
    protected_paths = list(args.protect)
    if not args.no_default_protect:
        protected_paths += DEFAULT_PROTECTED_PATHS

    if protected_paths:
        print(f"Protected (never flagged): {', '.join(protected_paths)}")

    unused, all_assets = analyze(project_root, args.min_size, args.exclude_scripts, protected_paths)
    print_report(unused, len(all_assets), project_root)

    if args.csv:
        write_csv(unused, project_root, Path(args.csv))
    if args.json:
        write_json(unused, project_root, Path(args.json))


if __name__ == "__main__":
    main()