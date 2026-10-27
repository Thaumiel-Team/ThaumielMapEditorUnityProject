"""
Options:
    --source-root PATH     Project Assets folder to scan (asked for at startup
                           if not given)
    --dest-root PATH       Folder the package folder is created in
    --package-name NAME    Name of the package folder (default: the name of the
                           directory you type)
    --no-dependencies     Only move the directory's own files, don't pull in
                           the assets they use
    --dry-run              Preview everything without touching any files
    --overwrite            Replace files that already exist at the destination
    --yes                  Skip the confirmation prompt

At prompt:
    <path to directory>    move that directory and its dependencies
    reload                 re-scan the source folder
    quit / exit / q        stop
"""

import argparse
import re
import shutil
from pathlib import Path


GUID_RE = re.compile(rb"guid:\s*([0-9a-fA-F]{32})")
ZERO_GUID = "0" * 32
META_SUFFIX = ".meta"

CODE_EXTENSIONS = {
    ".cs", ".dll", ".asmdef", ".asmref", ".rsp", ".so", ".dylib", ".jslib",
    ".jar", ".aar", ".mm", ".m", ".cpp", ".c", ".h", ".dllmeta",
}

def is_meta(path: Path) -> bool:
    return path.suffix.lower() == META_SUFFIX


def owner_of(path: Path) -> Path:
    return path.with_name(path.name[: -len(META_SUFFIX)]) if is_meta(path) else path


def is_yaml(path: Path) -> bool:
    try:
        with path.open("rb") as handle:
            return handle.read(5) == b"%YAML"
    except OSError:
        return False


def display(asset: Path, bases) -> str:
    for base in bases:
        try:
            return str(asset.relative_to(base))
        except ValueError:
            continue
    return str(asset)

def build_index(roots):
    print("Indexing assets in:")
    for root in roots:
        print(f"  {root}")

    all_files = set()
    for root in roots:
        if not root.exists():
            print(f"  WARNING: folder does not exist: {root}")
            continue
        all_files.update(p for p in root.rglob("*") if p.is_file())
    all_files = sorted(all_files)

    metas = [p for p in all_files if is_meta(p)]
    print(f"  Found {len(all_files)} file(s), {len(metas)} .meta file(s)")

    guid_to_asset = {}
    for meta_path in metas:
        asset = owner_of(meta_path)
        if not asset.is_file():
            continue
        match = GUID_RE.search(meta_path.read_bytes())
        if match:
            guid_to_asset[match.group(1).decode().lower()] = asset

    print(f"  {len(guid_to_asset)} asset(s) have a GUID")
    print("  Scanning for references...")

    referenced_by = {}
    references = {}
    for i, path in enumerate(all_files, 1):
        if i % 5000 == 0:
            print(f"    {i}/{len(all_files)}")
        if not is_meta(path) and not is_yaml(path):
            continue

        owner = owner_of(path)
        for raw in set(GUID_RE.findall(path.read_bytes())):
            guid = raw.decode().lower()
            if guid == ZERO_GUID:
                continue
            target = guid_to_asset.get(guid)
            if target is None or target == owner:
                continue
            referenced_by.setdefault(target, set()).add(owner)
            references.setdefault(owner, set()).add(target)

    print(f"  -> reference map built for {len(referenced_by)} referenced asset(s)\n")
    return guid_to_asset, referenced_by, references

def find_dependencies(start: set, references: dict, exclude: set) -> set:
    found = set()
    stack = list(start)
    while stack:
        asset = stack.pop()
        for dep in references.get(asset, ()):
            if dep in found or dep in exclude:
                continue
            if dep.suffix.lower() in CODE_EXTENSIONS:
                continue
            found.add(dep)
            stack.append(dep)
    return found


def plan_move(directory: Path, referenced_by: dict, references: dict,
              include_dependencies: bool, already_in_place: bool):
    dir_files = {p for p in directory.rglob("*") if p.is_file() and not is_meta(p)}

    if already_in_place:
        placed = set(dir_files)
        candidates = set()
    else:
        placed = set()
        candidates = set(dir_files)

    if include_dependencies:
        candidates |= find_dependencies(dir_files, references, exclude=dir_files)

    moving = set(candidates)
    changed = True
    while changed:
        changed = False
        group = moving | placed
        for asset in list(moving):
            if any(ref not in group for ref in referenced_by.get(asset, ())):
                moving.discard(asset)
                changed = True

    group = moving | placed
    blocked = {}
    for asset in candidates - moving:
        blocked[asset] = sorted(
            ref for ref in referenced_by.get(asset, ()) if ref not in group
        )
    return moving, blocked, placed, dir_files


def find_left_behind(group: set, references: dict) -> dict:
    left_behind = {}
    for asset in group:
        for dep in references.get(asset, ()):
            if dep not in group:
                left_behind.setdefault(dep, set()).add(asset)
    return left_behind


def relative_to_root(asset: Path, source_root: Path, directory: Path) -> Path:
    try:
        return asset.relative_to(source_root)
    except ValueError:
        return Path(directory.name) / asset.relative_to(directory)


def build_destinations(directory: Path, dest_dir: Path, source_root: Path,
                       moving: set, guid_of: dict) -> dict:
    dest_map = {}
    used = set()

    for asset in sorted(moving):
        dest = dest_dir / relative_to_root(asset, source_root, directory)
        if str(dest).lower() in used:
            tag = (guid_of.get(asset) or "dup")[:8]
            dest = dest.with_name(f"{dest.stem}_{tag}{dest.suffix}")
        used.add(str(dest).lower())
        dest_map[asset] = dest

    return dest_map


def collect_folder_metas(dest_map: dict, dest_dir: Path, overwrite: bool) -> dict:
    metas = {}
    for asset, dest_asset in dest_map.items():
        if dest_asset.exists() and not overwrite:
            continue

        parts = dest_asset.relative_to(dest_dir).parts
        n = len(parts)
        for i in range(1, n):
            src_folder = asset.parents[n - 1 - i]
            dest_folder = dest_dir.joinpath(*parts[:i])
            src_meta = src_folder.with_name(src_folder.name + META_SUFFIX)
            dest_meta = dest_folder.with_name(dest_folder.name + META_SUFFIX)
            if src_meta.exists() and not dest_meta.exists():
                metas[dest_meta] = src_meta
    return metas


def report_plan(bases, dir_files: set, moving: set, placed: set,
                blocked: dict, left_behind: dict):
    own = [a for a in moving if a in dir_files]
    deps = [a for a in moving if a not in dir_files]

    print(f"\n  Directory files to move:       {len(own)}"
          + (f"  ({len(placed)} already at the destination)" if placed else ""))
    print(f"  Dependencies pulled in:        {len(deps)}")
    print(f"  Staying (still used elsewhere): {len(blocked)}")

    if blocked:
        print("\n  Staying (something outside the move still uses them):")
        for asset in sorted(blocked)[:30]:
            users = blocked[asset]
            shown = ", ".join(u.name for u in users[:3])
            more = f" (+{len(users) - 3} more)" if len(users) > 3 else ""
            print(f"    {display(asset, bases)}  <-  {shown}{more}")
        if len(blocked) > 30:
            print(f"    ... and {len(blocked) - 30} more")

    if left_behind:
        print(f"\n  NOTE: the moving files still reference {len(left_behind)} asset(s) "
              f"that are not moving (scripts, or assets shared with other files). "
              f"They will be missing at the destination unless they exist there:")
        for dep in sorted(left_behind)[:20]:
            print(f"    {display(dep, bases)}")
        if len(left_behind) > 20:
            print(f"    ... and {len(left_behind) - 20} more")

def move_file_with_meta(asset: Path, dest_asset: Path, overwrite: bool, dry_run: bool) -> bool:
    src_meta = asset.with_name(asset.name + META_SUFFIX)
    dest_meta = dest_asset.with_name(dest_asset.name + META_SUFFIX)

    if dest_asset.exists() and not overwrite:
        print(f"    SKIPPED (already exists at destination): {dest_asset}")
        return False

    if dry_run:
        print(f"    [dry-run] {asset} -> {dest_asset}")
        return True

    dest_asset.parent.mkdir(parents=True, exist_ok=True)
    if dest_asset.exists():
        dest_asset.unlink()
    shutil.move(str(asset), str(dest_asset))

    if src_meta.exists():
        if dest_meta.exists():
            dest_meta.unlink()
        shutil.move(str(src_meta), str(dest_meta))
    else:
        print(f"    WARNING: no .meta next to {asset.name}; its GUID is not "
              f"preserved, so references to it will not relink.")
    return True


def remove_empty_parents(path: Path, stop_at: Path):
    stop = stop_at.resolve()
    parent = path.parent
    while True:
        try:
            resolved = parent.resolve()
        except OSError:
            return
        if resolved == stop or stop not in resolved.parents:
            return
        if not parent.exists() or any(parent.iterdir()):
            return

        meta = parent.with_name(parent.name + META_SUFFIX)
        parent.rmdir()
        if meta.exists():
            meta.unlink()
        parent = parent.parent


def cleanup_emptied_directories(directory: Path, source_root: Path):
    removed = 0
    folders = sorted(
        [p for p in directory.rglob("*") if p.is_dir()] + [directory],
        key=lambda p: len(p.parts),
        reverse=True,
    )
    for folder in folders:
        if not folder.exists() or any(folder.iterdir()):
            continue

        meta = folder.with_name(folder.name + META_SUFFIX)
        folder.rmdir()
        removed += 1
        if meta.exists():
            meta.unlink()

    if not directory.exists():
        remove_empty_parents(directory, source_root)
    return removed


def execute_move(directory: Path, dest_dir: Path, source_root: Path, dest_map: dict,
                 dir_files: set, overwrite: bool, dry_run: bool, already_in_place: bool):
    folder_metas = collect_folder_metas(dest_map, dest_dir, overwrite)
    if dry_run:
        if folder_metas:
            print(f"    [dry-run] {len(folder_metas)} folder .meta file(s) would be copied")
    else:
        for dest_meta, src_meta in sorted(folder_metas.items()):
            dest_meta.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src_meta, dest_meta)

    moved = 0
    skipped = 0
    for asset in sorted(dest_map):
        if move_file_with_meta(asset, dest_map[asset], overwrite, dry_run):
            moved += 1
            if not dry_run and asset not in dir_files:
                remove_empty_parents(asset, source_root)
        else:
            skipped += 1

    removed = 0
    if not dry_run and not already_in_place and any(a in dir_files for a in dest_map):
        removed = cleanup_emptied_directories(directory, source_root)

    print(f"\n  Done: {moved} file(s) moved, {len(folder_metas)} folder .meta file(s) "
          f"copied, {skipped} skipped, {removed} emptied folder(s) removed."
          + (" (dry run, nothing changed)" if dry_run else ""))
    return moved

def process_directory(directory: Path, source_root: Path, dest_root: Path, index,
                      package_name: str, include_dependencies: bool, overwrite: bool,
                      dry_run: bool, assume_yes: bool) -> bool:
    if not directory.exists() or not directory.is_dir():
        print(f"  Not a directory: {directory}")
        return False

    directory = directory.resolve()
    dest_dir = (dest_root / (package_name or directory.name)).resolve()
    already_in_place = directory == dest_dir

    inside_source = True
    try:
        directory.relative_to(source_root)
    except ValueError:
        inside_source = False

    if inside_source:
        if directory == source_root and include_dependencies:
            print("  NOTE: this directory is the same folder as the scan root, so "
                  "assets outside it can't be found. Use the project's whole Assets "
                  "folder as --source-root to pull in meshes, textures and shaders "
                  "that live elsewhere.")
        guid_to_asset, referenced_by, references = index
    else:
        print("  Directory is outside the source root; scanning it as well so "
              "its references to the project can be followed.")
        guid_to_asset, referenced_by, references = build_index([source_root, directory])

    print(f"\nPlanning move: {directory}\n           ->  {dest_dir}")
    if already_in_place:
        print("  (the directory is already at the destination; only its dependencies "
              "will be moved in)")

    moving, blocked, placed, dir_files = plan_move(
        directory, referenced_by, references, include_dependencies, already_in_place
    )
    if not dir_files:
        print("  No files found in that directory.")
        return False

    guid_of = {asset: guid for guid, asset in guid_to_asset.items()}
    dest_map = build_destinations(directory, dest_dir, source_root, moving, guid_of)
    left_behind = find_left_behind(moving | placed, references)
    report_plan([directory, source_root], dir_files, moving, placed,
                blocked, left_behind)

    if not moving:
        print("\n  Nothing to move.")
        return False

    if not dry_run and not assume_yes:
        answer = input(f"\n  Move {len(moving)} file(s)? [y/N] ").strip().lower()
        if answer not in ("y", "yes"):
            print("  Cancelled.")
            return False

    print()
    moved = execute_move(directory, dest_dir, source_root, dest_map, dir_files,
                         overwrite, dry_run, already_in_place)
    return moved > 0 and not dry_run


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    default_dest = Path(r"C:\Users\Superuser\ThaumielMapEditor\Assets\Resources")

    parser.add_argument("--source-root", type=Path, default=None,
                        help="Project Assets folder to scan for references "
                             "(asked for at startup if not given)")
    parser.add_argument("--dest-root", type=Path, default=default_dest,
                        help="Folder the package folder is created in")
    parser.add_argument("--package-name", default=None,
                        help="Name of the package folder "
                             "(default: the name of the directory you type)")
    parser.add_argument("--no-dependencies", action="store_true",
                        help="Only move the directory's own files")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--overwrite", action="store_true",
                        help="Replace files that already exist at the destination")
    parser.add_argument("--yes", action="store_true",
                        help="Skip the confirmation prompt")
    args = parser.parse_args()

    if args.source_root is None:
        try:
            entered = input("Project Assets folder to scan for references: ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nExiting.")
            return
        if not entered:
            print("No source folder given. Pass --source-root or enter a path.")
            return
        args.source_root = Path(entered.strip('"').strip("'"))

    source_root = args.source_root.resolve()
    index = build_index([source_root])

    print("Type the path to a directory and press Enter to move it together with "
          "the assets it uses.")
    print("Commands: 'reload' to re-scan, 'quit'/'exit'/'q' to stop.\n")

    while True:
        try:
            line = input("move> ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nExiting.")
            break

        if not line:
            continue
        if line.lower() in ("quit", "exit", "q"):
            print("Exiting.")
            break
        if line.lower() == "reload":
            index = build_index([source_root])
            continue

        directory = Path(line.strip('"').strip("'"))
        changed = process_directory(
            directory, source_root, args.dest_root, index, args.package_name,
            not args.no_dependencies, args.overwrite, args.dry_run, args.yes,
        )

        if changed:
            print("\n  Files moved, rebuilding the reference map...")
            index = build_index([source_root])
        print()


if __name__ == "__main__":
    main()