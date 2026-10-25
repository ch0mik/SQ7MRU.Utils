"""Verify every platform's final container and combine checksums before release upload."""
import argparse
import hashlib
import re
from pathlib import Path

FORMATS = {
    'win-x86': 'zip', 'win-x64': 'zip', 'win-arm64': 'zip',
    'linux-x64': 'tar.gz', 'linux-arm64': 'tar.gz', 'linux-arm': 'tar.gz',
    'osx-x64': 'dmg', 'osx-arm64': 'dmg',
}
NAME = re.compile(
    r'SQ7MRU\.Downloader-(\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?)-'
    r'(win-x86|win-x64|win-arm64|linux-x64|linux-arm64|linux-arm|osx-x64|osx-arm64)\.(zip|tar\.gz|dmg)'
)


def write_manifest(directory: Path) -> None:
    seen = set()
    versions = set()
    lines = []
    files = sorted(directory.iterdir())
    packages = [p for p in files if p.name != 'SHA256SUMS' and not p.name.endswith('.sha256')]
    for package in packages:
        match = NAME.fullmatch(package.name)
        if not match or not package.is_file() or package.is_symlink():
            raise ValueError(f'Unexpected release asset: {package.name}')
        version, rid, extension = match.groups()
        if rid in seen or extension != FORMATS[rid]:
            raise ValueError(f'Duplicate platform or incorrect format: {package.name}')
        seen.add(rid)
        versions.add(version)
        with package.open('rb') as stream:
            digest = hashlib.file_digest(stream, 'sha256').hexdigest()
        expected = f'{digest}  {package.name}'
        sidecar = directory / (package.name + '.sha256')
        if sidecar.is_symlink() or sidecar.read_text(encoding='ascii').strip() != expected:
            raise ValueError(f'Checksum mismatch: {package.name}')
        lines.append(expected)
    if seen != set(FORMATS) or len(versions) != 1:
        raise ValueError('Expected exactly eight platform packages from the same version.')
    expected_names = {p.name + '.sha256' for p in packages}
    if {p.name for p in files if p.name.endswith('.sha256')} != expected_names:
        raise ValueError('Unexpected checksum sidecars.')
    (directory / 'SHA256SUMS').write_text('\n'.join(lines) + '\n', encoding='ascii', newline='\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    args = parser.parse_args()
    write_manifest(args.directory)
    print('Verified eight desktop packages and wrote SHA256SUMS.')
