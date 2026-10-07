"""
Options:
    --source-root PATH        AssetRipper ExportedProject/Assets folder
    --dest-root PATH          Root folder to copy matched assets into
    --dry-run                 Preview actions without copying files

At prompt:
    <path to .prefab>       process that prefab
    <path to directory>     process all .prefab files in that directory (recursive)
    reload                  re-scan the source folders (if you added new assets)
    quit / exit / q         stop
"""

import argparse
import re
import shutil
from pathlib import Path


GUID_RE = re.compile(r"guid:\s*([0-9a-fA-F]{32})")

MATERIAL_BLOCK_RE = re.compile(
    r"m_Materials:\s*\n((?:\s*-\s*\{fileID:.*\n?)*)", re.MULTILINE
)
FILEID_GUID_TYPE_RE = re.compile(
    r"\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*(\d+)\}"
)
MESH_REF_RE = re.compile(
    r"m_Mesh:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*(\d+)\}"
)
TEXTURE_REF_RE = re.compile(
    r"m_Texture:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*(\d+)\}"
)
SHADER_LINE_RE = re.compile(r"m_Shader:\s*\{[^}]*\}")

ZERO_GUID = "0" * 32


def find_material_guids(text: str) -> set:
    guids = set()
    for block in MATERIAL_BLOCK_RE.findall(text):
        for _fileid, guid, _type in FILEID_GUID_TYPE_RE.findall(block):
            if guid.lower() != ZERO_GUID:
                guids.add(guid.lower())
    if guids:
        return guids
    for _fileid, guid, type_ in FILEID_GUID_TYPE_RE.findall(text):
        if type_ == "2" and guid.lower() != ZERO_GUID:
            guids.add(guid.lower())
    return guids


def find_mesh_guids(text: str) -> set:
    return {
        guid.lower()
        for _fileid, guid, _type in MESH_REF_RE.findall(text)
        if guid.lower() != ZERO_GUID
    }


def find_texture_guids(text: str) -> set:
    return {
        guid.lower()
        for _fileid, guid, _type in TEXTURE_REF_RE.findall(text)
        if guid.lower() != ZERO_GUID
    }


def find_shader_guids(text: str) -> set:
    guids = set()
    for match in SHADER_LINE_RE.finditer(text):
        guid_match = GUID_RE.search(match.group(0))
        if guid_match:
            guid = guid_match.group(1).lower()
            if guid != ZERO_GUID:
                guids.add(guid)
    return guids


def index_all_assets_by_guid(assets_dir: Path) -> dict:
    guid_to_asset = {}
    if not assets_dir.exists():
        print(f"  WARNING: folder does not exist, skipping: {assets_dir}")
        return guid_to_asset

    meta_files = list(assets_dir.rglob("*.meta"))
    print(f"    Found {len(meta_files)} .meta file(s) to scan...")

    for meta_path in meta_files:
        asset_path = meta_path.with_name(meta_path.name[: -len(".meta")])
        if not asset_path.exists() or asset_path.is_dir():
            continue
        meta_text = meta_path.read_text(encoding="utf-8", errors="ignore")
        match = GUID_RE.search(meta_text)
        if match:
            guid_to_asset[match.group(1).lower()] = asset_path

    return guid_to_asset


def copy_asset(asset_path: Path, dest_dir: Path, dry_run: bool = False) -> Path:
    dest_asset = dest_dir / asset_path.name
    src_meta = asset_path.with_name(asset_path.name + ".meta")
    dest_meta = dest_dir / src_meta.name

    if dry_run:
        print(f"    [dry-run] {asset_path} -> {dest_asset}")
        if src_meta.exists():
            print(f"    [dry-run] {src_meta} -> {dest_meta}")
        return dest_asset

    dest_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(asset_path, dest_asset)
    if src_meta.exists():
        shutil.copy2(src_meta, dest_meta)
    else:
        print(
            f"    WARNING: no .meta file found next to {asset_path.name}; "
            f"its GUID won't be preserved, so Unity will treat this as a "
            f"brand-new asset and existing references will NOT relink."
        )
    return dest_asset


def process_category(name, guids, guid_index, dest_dir, dry_run):
    copied = []
    missing = []
    for guid in sorted(guids):
        asset_path = guid_index.get(guid)
        if asset_path is None:
            missing.append(guid)
            continue
        print(f"  Copying {name[:-1]} for guid {guid}: {asset_path.name}")
        dest = copy_asset(asset_path, dest_dir, dry_run=dry_run)
        copied.append((guid, asset_path, dest))
    return copied, missing


def process_prefab(prefab_path: Path, guid_index: dict, dest_dir: Path, dry_run: bool):
    if not prefab_path.exists():
        print(f"  Prefab not found: {prefab_path}")
        return

    prefab_text = prefab_path.read_text(encoding="utf-8", errors="ignore")
    material_guids = find_material_guids(prefab_text)
    mesh_guids = find_mesh_guids(prefab_text)
    print(f"  Found {len(material_guids)} material reference(s), "
          f"{len(mesh_guids)} mesh reference(s)")

    print("  Materials:")
    copied_materials, missing_materials = process_category(
        "materials", material_guids, guid_index, dest_dir / "Materials", dry_run
    )

    print("  Meshes:")
    copied_meshes, missing_meshes = process_category(
        "meshes", mesh_guids, guid_index, dest_dir / "Meshes", dry_run
    )

    shader_guids = set()
    for _guid, src_mat_path, _dest in copied_materials:
        mat_text = src_mat_path.read_text(encoding="utf-8", errors="ignore")
        shader_guids |= find_shader_guids(mat_text)

    print(f"  Found {len(shader_guids)} shader reference(s) in matched materials.")
    print("  Shaders:")
    copied_shaders, missing_shaders = process_category(
        "shaders", shader_guids, guid_index, dest_dir / "Shaders", dry_run
    )

    texture_guids = set()
    for _guid, src_mat_path, _dest in copied_materials:
        mat_text = src_mat_path.read_text(encoding="utf-8", errors="ignore")
        texture_guids |= find_texture_guids(mat_text)

    print(f"  Found {len(texture_guids)} texture reference(s) in matched materials.")
    print("  Textures:")
    copied_textures, missing_textures = process_category(
        "textures", texture_guids, guid_index, dest_dir / "Textures", dry_run
    )

    print(
        f"\n  Summary for {prefab_path.name}: "
        f"{len(copied_materials)} material(s), {len(copied_meshes)} mesh(es), "
        f"{len(copied_shaders)} shader(s), {len(copied_textures)} texture(s) copied."
    )
    for kind, missing in (
        ("material", missing_materials),
        ("mesh", missing_meshes),
        ("shader", missing_shaders),
        ("texture", missing_textures),
    ):
        if missing:
            print(f"  Missing {kind}(s) (probably Unity built-ins): "
                  f"{', '.join(missing)}")


def find_prefabs_in_directory(dir_path: Path) -> list:
    if not dir_path.exists():
        print(f"  Directory not found: {dir_path}")
        return []
    if not dir_path.is_dir():
        print(f"  Not a directory: {dir_path}")
        return []
    return sorted(dir_path.rglob("*.prefab"))


def process_input(input_path: Path, guid_index: dict, dest_dir: Path, dry_run: bool):
    if not input_path.exists():
        print(f"  Path not found: {input_path}")
        return

    if input_path.is_file() and input_path.suffix.lower() == ".prefab":
        prefabs = [input_path]
    elif input_path.is_dir():
        prefabs = find_prefabs_in_directory(input_path)
        if not prefabs:
            print(f"  No .prefab files found in: {input_path}")
            return
        print(f"\nFound {len(prefabs)} .prefab file(s) in {input_path}\n")
    else:
        print(f"  Not a .prefab file or directory: {input_path}")
        return

    for i, prefab_path in enumerate(prefabs, 1):
        if len(prefabs) > 1:
            print(f"[{i}/{len(prefabs)}] Processing: {prefab_path}")
        else:
            print(f"\nProcessing: {prefab_path}")
        process_prefab(prefab_path, guid_index, dest_dir, dry_run)
        if i < len(prefabs):
            print("-" * 50)


def build_index(src_dir: Path):
    print(f"Indexing ALL assets in: {src_dir}")
    print("  (this only happens once -- every GUID across every subfolder is loaded into memory)")
    guid_index = index_all_assets_by_guid(src_dir)
    print(f"  -> {len(guid_index)} total asset(s) indexed\n")
    return guid_index


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    default_export_assets = Path(
        r"D:\Downloads\ExportedSL\ExportedProject\Assets"
    )
    default_dest_root = Path(r"C:\Users\Superuser\ThaumielMapEditor\Assets\Resources")

    parser.add_argument("--source-root", type=Path, default=default_export_assets, help="Root Assets folder to scan (default: AssetRipper export)")
    parser.add_argument("--dest-root", type=Path, default=default_dest_root, help="Root folder to copy assets into (default: project Resources)")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    guid_index = build_index(args.source_root)

    print("Type the path to a .prefab file or directory and press Enter to process it.")
    print("Commands: 'reload' to re-scan source folders, 'quit'/'exit'/'q' to stop.\n")

    while True:
        try:
            line = input("prefab> ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nExiting.")
            break

        if not line:
            continue
        if line.lower() in ("quit", "exit", "q"):
            print("Exiting.")
            break
        if line.lower() == "reload":
            guid_index = build_index(args.source_root)
            continue

        input_path = Path(line.strip('"').strip("'"))
        process_input(input_path, guid_index, args.dest_root, args.dry_run)
        print()


if __name__ == "__main__":
    main()