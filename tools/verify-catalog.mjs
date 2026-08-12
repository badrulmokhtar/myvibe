import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const versionPattern = /^\d+\.\d+\.\d+$/;
const shaPattern = /^[a-f\d]{64}$/i;

function verifyPackage(value, channel, label) {
  assert(value && typeof value === 'object', `${label} must be an object`);
  assert(versionPattern.test(value.version), `${label}.version must use x.y.z`);
  assert.equal(value.platform, 'windows', `${label}.platform must be windows`);
  assert.equal(value.architecture, 'x64', `${label}.architecture must be x64`);
  assert(shaPattern.test(value.sha256), `${label}.sha256 must contain 64 hexadecimal characters`);
  const url = new URL(value.downloadUrl);
  assert.equal(url.protocol, 'https:', `${label}.downloadUrl must use HTTPS`);
  assert.equal(url.hostname, 'github.com', `${label}.downloadUrl must use github.com`);
  assert(url.pathname.includes('/releases/download/'), `${label}.downloadUrl must target a versioned GitHub Release asset`);
  assert.equal(typeof value.authenticodeRequired, 'boolean', `${label}.authenticodeRequired must be boolean`);
  if (channel === 'stable') assert.equal(value.authenticodeRequired, true, `${label} must require Authenticode on the stable channel`);
}

export function verifyCatalog(catalog) {
  assert.equal(catalog.schemaVersion, 1, 'schemaVersion must be 1');
  assert(['beta', 'stable'].includes(catalog.channel), 'channel must be beta or stable');
  verifyPackage(catalog.manager, catalog.channel, 'manager');
  assert(Array.isArray(catalog.plugins) && catalog.plugins.length > 0, 'plugins must contain at least one entry');
  const ids = new Set();
  for (const [index, plugin] of catalog.plugins.entries()) {
    const label = `plugins[${index}]`;
    verifyPackage(plugin, catalog.channel, label);
    assert(plugin.id && !ids.has(plugin.id), `${label}.id must be unique`);
    ids.add(plugin.id);
    assert(plugin.name && plugin.description, `${label} requires a name and description`);
    assert.equal(plugin.host, 'Adobe Illustrator', `${label}.host is unsupported`);
    assert.equal(plugin.hostVersion, '30.x', `${label}.hostVersion is unsupported`);
    assert(versionPattern.test(plugin.minimumManagerVersion), `${label}.minimumManagerVersion must use x.y.z`);
  }
}

if (process.argv.includes('--self-test')) {
  const sample = {
    schemaVersion: 1,
    channel: 'beta',
    manager: { version: '1.0.0', platform: 'windows', architecture: 'x64', downloadUrl: 'https://github.com/example/repo/releases/download/v1/file.zip', sha256: 'a'.repeat(64), authenticodeRequired: false },
    plugins: [{ id: 'example', name: 'Example', description: 'Example', version: '1.0.0', host: 'Adobe Illustrator', hostVersion: '30.x', minimumManagerVersion: '1.0.0', platform: 'windows', architecture: 'x64', downloadUrl: 'https://github.com/example/repo/releases/download/v1/plugin.zip', sha256: 'b'.repeat(64), authenticodeRequired: false }]
  };
  verifyCatalog(sample);
  assert.throws(() => verifyCatalog({ ...sample, channel: 'stable' }));
  console.log('Catalog verifier self-test passed.');
} else {
  verifyCatalog(JSON.parse(readFileSync(new URL('../catalog.json', import.meta.url), 'utf8')));
  console.log('Catalog verified.');
}
