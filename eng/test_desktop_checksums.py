"""Exercise release promotion using synthetic byte payloads, not real application archives."""
import hashlib
import tempfile
import unittest
from pathlib import Path

from desktop_checksums import FORMATS, write_manifest


class DesktopChecksumsTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        # A synthetic prerelease exercises version parsing without a product release number.
        for rid, extension in FORMATS.items():
            self.add_package(rid, extension, '0.0.0-fixture')

    def add_package(self, rid, extension, version):
        package = self.directory / f'SQ7MRU.Downloader-{version}-{rid}.{extension}'
        payload = f'synthetic payload for {rid}'.encode('ascii')
        package.write_bytes(payload)
        digest = hashlib.sha256(payload).hexdigest()
        Path(str(package) + '.sha256').write_text(f'{digest}  {package.name}\n', encoding='ascii')
        return package

    def test_complete_set_produces_reproducible_manifest(self):
        write_manifest(self.directory)
        manifest = (self.directory / 'SHA256SUMS').read_bytes()
        self.assertEqual(8, len(manifest.splitlines()))
        self.assertNotIn(b'\r', manifest)
        write_manifest(self.directory)
        self.assertEqual(manifest, (self.directory / 'SHA256SUMS').read_bytes())

    def test_changed_package_blocks_manifest(self):
        next(self.directory.glob('*.zip')).write_bytes(b'corrupted')
        with self.assertRaisesRegex(ValueError, 'Checksum mismatch'):
            write_manifest(self.directory)
        self.assertFalse((self.directory / 'SHA256SUMS').exists())

    def test_missing_platform_blocks_manifest(self):
        package = next(self.directory.glob('*.dmg'))
        package.unlink()
        Path(str(package) + '.sha256').unlink()
        with self.assertRaisesRegex(ValueError, 'exactly eight'):
            write_manifest(self.directory)

    def test_different_versions_cannot_be_promoted_together(self):
        package = next(self.directory.glob('*-linux-arm.tar.gz'))
        package.unlink()
        Path(str(package) + '.sha256').unlink()
        self.add_package('linux-arm', 'tar.gz', '0.0.0-other-fixture')
        with self.assertRaisesRegex(ValueError, 'same version'):
            write_manifest(self.directory)

    def test_unexpected_asset_blocks_manifest(self):
        (self.directory / 'unapproved.txt').write_text('unexpected')
        with self.assertRaisesRegex(ValueError, 'Unexpected release asset'):
            write_manifest(self.directory)

    def test_wrong_platform_format_blocks_manifest(self):
        package = next(self.directory.glob('*-osx-x64.dmg'))
        package.unlink()
        Path(str(package) + '.sha256').unlink()
        self.add_package('osx-x64', 'zip', '0.0.0-fixture')
        with self.assertRaisesRegex(ValueError, 'incorrect format'):
            write_manifest(self.directory)


if __name__ == '__main__':
    unittest.main()
