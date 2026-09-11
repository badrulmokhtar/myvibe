import assert from 'node:assert/strict';
import { createPublicKey, verify } from 'node:crypto';
import { readFileSync } from 'node:fs';

const schema = JSON.parse(readFileSync(new URL('../catalog-v2.schema.json', import.meta.url), 'utf8'));
assert.equal(schema.properties?.schemaVersion?.const, 2, 'catalog-v2.schema.json must describe schemaVersion 2');

const versionPattern = /^\d+\.\d+\.\d+$/;
const shaPattern = /^[a-f\d]{64}$/i;
const teamPattern = /^[A-Z0-9]{10}$/;
const thumbprintPattern = /^[A-F\d]{40,64}$/i;

function verifyArtifact(artifact, channel, label) {
  assert(artifact && typeof artifact === 'object', `${label} must be an object`);
  assert(['windows', 'macos'].includes(artifact.platform), `${label}.platform is unsupported`);
  assert(['x64', 'arm64', 'universal'].includes(artifact.architecture), `${label}.architecture is unsupported`);
  assert(!(artifact.platform === 'windows' && artifact.architecture === 'universal'), `${label} cannot use universal Windows architecture`);
  assert(shaPattern.test(artifact.sha256), `${label}.sha256 must contain 64 hexadecimal characters`);

  const url = new URL(artifact.downloadUrl);
  assert.equal(url.protocol, 'https:', `${label}.downloadUrl must use HTTPS`);
  assert.equal(url.hostname, 'github.com', `${label}.downloadUrl must use github.com`);
  assert(url.pathname.toLowerCase().startsWith('/badrulmokhtar/myvibe/releases/download/'), `${label}.downloadUrl must target the official MyVibe release repository`);

  assert(artifact.signature && typeof artifact.signature === 'object', `${label}.signature is required`);
  assert(['authenticode', 'developer-id', 'adobe-cep'].includes(artifact.signature.type), `${label}.signature.type is unsupported`);
  if (artifact.signature.type === 'authenticode') assert.equal(artifact.platform, 'windows', `${label} Authenticode is Windows-only`);
  if (artifact.signature.type === 'developer-id') assert.equal(artifact.platform, 'macos', `${label} Developer ID is macOS-only`);
  assert.equal(typeof artifact.signature.required, 'boolean', `${label}.signature.required must be boolean`);
  if (channel === 'stable') {
    assert.equal(artifact.signature.required, true, `${label} must require a trusted signature on stable`);
    if (artifact.signature.type === 'authenticode') assert(thumbprintPattern.test(artifact.signature.signerThumbprint), `${label}.signature.signerThumbprint is required on stable`);
    if (artifact.signature.type === 'developer-id') assert(teamPattern.test(artifact.signature.teamIdentifier), `${label}.signature.teamIdentifier is required on stable`);
  }
  if (artifact.signature.signerThumbprint !== undefined) assert(thumbprintPattern.test(artifact.signature.signerThumbprint), `${label}.signature.signerThumbprint is invalid`);
  if (artifact.signature.teamIdentifier !== undefined) assert(teamPattern.test(artifact.signature.teamIdentifier), `${label}.signature.teamIdentifier is invalid`);
}

function verifyProduct(product, channel, label) {
  assert(product && typeof product === 'object', `${label} must be an object`);
  assert(product.id && product.name, `${label} requires id and name`);
  assert(versionPattern.test(product.version), `${label}.version must use x.y.z`);
  assert(Array.isArray(product.artifacts) && product.artifacts.length > 0, `${label}.artifacts must not be empty`);
  const targets = new Set();
  for (const [index, artifact] of product.artifacts.entries()) {
    const artifactLabel = `${label}.artifacts[${index}]`;
    verifyArtifact(artifact, channel, artifactLabel);
    const target = `${artifact.platform}/${artifact.architecture}`;
    assert(!targets.has(target), `${label} has duplicate ${target} artifacts`);
    targets.add(target);
  }
  if (product.releases !== undefined) {
    assert(Array.isArray(product.releases) && product.releases.length > 0, `${label}.releases must not be empty`);
    const versions = new Set();
    let previous = null;
    for (const [index, release] of product.releases.entries()) {
      const releaseLabel = `${label}.releases[${index}]`;
      assert(versionPattern.test(release?.version), `${releaseLabel}.version must use x.y.z`);
      assert(/^\d{4}-\d{2}-\d{2}$/.test(release.releasedAt), `${releaseLabel}.releasedAt must use YYYY-MM-DD`);
      assert(!versions.has(release.version), `${label} has duplicate release ${release.version}`);
      versions.add(release.version);
      if (previous) assert(previous.localeCompare(release.version, undefined, { numeric: true }) > 0, `${label}.releases must be newest first`);
      previous = release.version;
      verifyProduct({ id: product.id, name: product.name, version: release.version, artifacts: release.artifacts }, channel, releaseLabel);
    }
    assert.equal(product.releases[0].version, product.version, `${label}.releases[0] must be the latest version`);
    assert.deepEqual(product.releases[0].artifacts, product.artifacts, `${label} latest release artifacts must match top-level artifacts`);
  }
}

export function verifyCatalogV2(catalog) {
  assert.equal(catalog.schemaVersion, 2, 'schemaVersion must be 2');
  assert(['beta', 'stable'].includes(catalog.channel), 'channel must be beta or stable');
  assert.equal(catalog.manager?.id, 'com.badru.myvibe', 'manager.id must identify MyVibe');
  verifyProduct(catalog.manager, catalog.channel, 'manager');
  assert(Array.isArray(catalog.plugins) && catalog.plugins.length > 0, 'plugins must contain at least one entry');
  const ids = new Set([catalog.manager.id]);
  for (const [index, plugin] of catalog.plugins.entries()) {
    const label = `plugins[${index}]`;
    verifyProduct(plugin, catalog.channel, label);
    assert(!ids.has(plugin.id), `${label}.id must be unique`);
    ids.add(plugin.id);
    assert(plugin.description, `${label}.description is required`);
    assert.equal(plugin.host, 'Adobe Illustrator', `${label}.host is unsupported`);
    assert.equal(plugin.hostVersion, '30.x', `${label}.hostVersion is unsupported`);
    assert(versionPattern.test(plugin.minimumManagerVersion), `${label}.minimumManagerVersion must use x.y.z`);
  }
}

const sample = {
  schemaVersion: 2,
  channel: 'beta',
  manager: {
    id: 'com.badru.myvibe', name: 'MyVibe', version: '0.4.0',
    artifacts: [
      { platform: 'windows', architecture: 'x64', downloadUrl: 'https://github.com/badrulmokhtar/myvibe/releases/download/v0/windows.zip', sha256: 'a'.repeat(64), signature: { type: 'authenticode', required: false } },
      { platform: 'macos', architecture: 'universal', downloadUrl: 'https://github.com/badrulmokhtar/myvibe/releases/download/v0/macos.zip', sha256: 'b'.repeat(64), signature: { type: 'developer-id', required: false } }
    ]
  },
  plugins: [{
    id: 'com.badru.example', name: 'Example', description: 'Example', version: '1.0.0', host: 'Adobe Illustrator', hostVersion: '30.x', minimumManagerVersion: '0.4.0',
    artifacts: [{ platform: 'macos', architecture: 'universal', downloadUrl: 'https://github.com/badrulmokhtar/myvibe/releases/download/v0/plugin.zip', sha256: 'c'.repeat(64), signature: { type: 'developer-id', required: false } }]
  }]
};

if (process.argv.includes('--self-test')) {
  verifyCatalogV2(sample);
  const history = structuredClone(sample);
  history.plugins[0].releases = [{ version: '1.0.0', releasedAt: '2026-09-11', artifacts: history.plugins[0].artifacts }];
  verifyCatalogV2(history);
  assert.throws(() => verifyCatalogV2({ ...history, plugins: [{ ...history.plugins[0], releases: [{ ...history.plugins[0].releases[0], version: '0.9.0' }] }] }));
  const stable = structuredClone(sample);
  stable.channel = 'stable';
  for (const product of [stable.manager, ...stable.plugins]) {
    for (const artifact of product.artifacts) {
      artifact.signature.required = true;
      if (artifact.platform === 'windows') artifact.signature.signerThumbprint = 'A'.repeat(40);
      if (artifact.platform === 'macos') artifact.signature.teamIdentifier = 'A1B2C3D4E5';
    }
  }
  verifyCatalogV2(stable);
  assert.throws(() => verifyCatalogV2({ ...sample, channel: 'stable' }));
  assert.throws(() => verifyCatalogV2({ ...sample, manager: { ...sample.manager, artifacts: [sample.manager.artifacts[0], sample.manager.artifacts[0]] } }));
  assert.throws(() => verifyCatalogV2({ ...sample, plugins: [{ ...sample.plugins[0], artifacts: [{ ...sample.plugins[0].artifacts[0], downloadUrl: 'https://github.com/attacker/release.zip' }] }] }));
  console.log('Cross-platform catalog verifier self-test passed.');
} else {
  const catalogPath = process.argv[2];
  const signaturePath = process.argv[3];
  assert(catalogPath && signaturePath, 'Usage: node tools/verify-catalog-v2.mjs <catalog-v2.json> <catalog-v2.json.sig>');
  const catalogBytes = readFileSync(catalogPath);
  const signature = Buffer.from(readFileSync(signaturePath, 'utf8').trim(), 'base64');
  const publicKey = createPublicKey(readFileSync(new URL('../macos/catalog-v2-public-key.pem', import.meta.url)));
  assert(verify('sha256', catalogBytes, publicKey, signature), 'catalog v2 signature is invalid');
  verifyCatalogV2(JSON.parse(catalogBytes.toString('utf8')));
  console.log('Cross-platform catalog signature and metadata verified.');
}
