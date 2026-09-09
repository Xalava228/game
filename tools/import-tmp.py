"""Unpack Unity's official TMP essentials before a non-interactive editor build."""
import tarfile
from pathlib import Path
project = Path(__file__).resolve().parents[1] / 'game'
package = Path(r'D:/UnityHub/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage')
count = 0
with tarfile.open(package, 'r:gz') as tar:
    entries = {member.name: member for member in tar.getmembers()}
    for name, entry in entries.items():
        if not name.endswith('/pathname'):
            continue
        relative = tar.extractfile(entry).read().decode('utf-8').strip()
        destination = (project / relative).resolve()
        allowed = (project / 'Assets' / 'TextMesh Pro').resolve()
        if destination != allowed and allowed not in destination.parents:
            raise RuntimeError(f'Unexpected TMP package path: {relative}')
        base = name.rsplit('/', 1)[0]
        if base + '/asset' in entries:
            destination.parent.mkdir(parents=True, exist_ok=True)
            if not destination.exists():
                destination.write_bytes(tar.extractfile(entries[base + '/asset']).read())
                count += 1
        else:
            destination.mkdir(parents=True, exist_ok=True)
        if base + '/asset.meta' in entries:
            meta = Path(str(destination) + '.meta')
            if not meta.exists():
                meta.write_bytes(tar.extractfile(entries[base + '/asset.meta']).read())
print(f'Imported {count} official TMP resource files into {project / "Assets"}')
