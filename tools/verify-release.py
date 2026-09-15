"""Verify the upload archive against the exact WebGL files that passed browser checks."""
from pathlib import Path
import hashlib
import json
import re
import zipfile

repo = Path(__file__).resolve().parent.parent
build = repo / 'Builds' / 'WebGL'
archive = Path('D:/Game/Release/TimeThief-Yandex-1.7.0.zip')
expected = {str(p.relative_to(build)).replace('\\', '/'): p for p in build.rglob('*') if p.is_file()}
with zipfile.ZipFile(archive) as z:
    actual = {n for n in z.namelist() if not n.endswith('/')}
    assert actual == set(expected), f'Archive contents differ: {actual ^ set(expected)}'
    assert 'index.html' in actual and 'sdk-bridge.js' in actual and 'icon.png' in actual
    assert all('\\' not in n and not n.startswith('/') and '..' not in n.split('/') for n in actual)
    assert all(re.fullmatch(r'[A-Za-z0-9_./-]+', n) for n in actual), 'Unsafe archive filename'
    assert sum(i.file_size for i in z.infolist()) <= 100 * 1024 * 1024, 'Yandex uncompressed size limit'
    assert z.testzip() is None, 'ZIP CRC validation failed'
    for name, path in expected.items():
        assert hashlib.sha256(z.read(name)).digest() == hashlib.sha256(path.read_bytes()).digest(), name
    assert z.read('sdk-bridge.js') == (repo / 'game/Assets/WebGLTemplates/TimeThief/sdk-bridge.js').read_bytes()
    assert z.read('icon.png') == (repo / 'game/Assets/TimeThief/Resources/Art/logo.png').read_bytes()
    html = z.read('index.html').decode('utf-8')
    assert "productVersion:'1.7.0'" in html
    referenced = set(re.findall(r'Build/[a-zA-Z0-9.\-]+', html))
    assert len(referenced) == 4 and referenced.issubset(actual), 'HTML references missing Unity files'
result = {'archive': str(archive), 'bytes': archive.stat().st_size,
          'sha256': hashlib.sha256(archive.read_bytes()).hexdigest(),
          'files': sorted(actual), 'checks': 'ZIP CRC, root index, safe names, byte-identical build files, four resolved Unity references'}
(repo / 'QA' / 'package-checks.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, indent=2))
