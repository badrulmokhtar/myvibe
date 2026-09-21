"""Read-only download check for the current signed catalog's Transform packages.
Run catalog signature verification before this script; no install or execution.
"""
import hashlib
import io
import json
from pathlib import Path
import struct
import urllib.request
import zipfile
import xml.etree.ElementTree as ET

catalog = json.loads((Path(__file__).resolve().parent.parent / 'catalog-v2.json').read_text())
plugin = next(item for item in catalog['plugins'] if item['id'] == 'com.badru.transform2d5')
for artifact in plugin['artifacts']:
    url = artifact['downloadUrl']
    assert url.startswith('https://github.com/badrulmokhtar/myvibe/releases/download/')
    with urllib.request.urlopen(url, timeout=120) as response:
        data = response.read(250 * 1024 * 1024 + 1)
    assert 0 < len(data) <= 250 * 1024 * 1024
    assert hashlib.sha256(data).hexdigest() == artifact['sha256'].lower(), 'Package checksum mismatch'
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        names = archive.namelist()
        assert all(not name.startswith('/') and '..' not in name.split('/') for name in names)
        manifest = ET.fromstring(archive.read('2.5D Transform/CSXS/manifest.xml'))
        assert manifest.attrib['ExtensionBundleVersion'] == plugin['version']
        assert len(manifest.findall('./ExtensionList/Extension')) == 1
        assert b'id="groupsTab"' in archive.read('2.5D Transform/index.html')
        if artifact['platform'] == 'windows':
            assert '2.5D Transform/META-INF/signatures.xml' in names, 'MyVibe requires signed CEP for native Windows plugins'
            native = archive.read('2.5D Transform.aip')
            assert native[:2] == b'MZ'
            pe = struct.unpack_from('<I', native, 0x3c)[0]
            assert native[pe:pe+4] == b'PE\0\0'
            assert struct.unpack_from('<H', native, pe+4)[0] == 0x8664
        else:
            executable = '2.5D Transform.aip/Contents/MacOS/2.5D Transform'
            native = archive.read(executable)
            assert native[:4] == b'\xca\xfe\xba\xbe', 'Expected universal Mach-O'
            count = struct.unpack_from('>I', native, 4)[0]
            architectures = {struct.unpack_from('>I', native, 8 + i * 20)[0] for i in range(count)}
            assert {0x01000007, 0x0100000c} <= architectures
            assert (archive.getinfo(executable).external_attr >> 16) & 0o111
    print(f"PASS: anonymous {artifact['platform']} download, SHA-256, {plugin['version']} manifest, tabs, native architecture and installer layout")
