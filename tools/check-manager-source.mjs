import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const windows = readFileSync(new URL('../windows/MyVibe.cs', import.meta.url), 'utf8');
const macos = readFileSync(new URL('../macos/MyVibe.swift', import.meta.url), 'utf8');
const workflow = readFileSync(new URL('../.github/workflows/windows-beta.yml', import.meta.url), 'utf8');

assert.match(windows, /com\.badru\.logolize/);
assert.match(windows, /HasNative/);
assert.match(windows, /adobe-cep/);
assert.match(windows, /CatalogReleaseV2/);
assert.match(windows, /Roll back to/);
assert.match(macos, /com\.badru\.logolize/);
assert.match(macos, /nativeName: String\?/);
assert.match(macos, /ProductRelease/);
assert.match(macos, /Roll Back/);
assert.match(workflow, /MyVibe-0\.5\.1-beta\.1-win-x64/);
console.log('Cross-platform manager source checks passed.');

assert.match(windows, /com\.badru\.autoops/);
assert.match(macos, /com\.badru\.autoops/);
