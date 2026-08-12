import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const forbiddenPath = /(^|\/)(?:\.env(?:\..+)?|\.codex|\.claude|\.cursor|\.continue|\.windsurf|ai-chat-history|chat-history)(?:\/|$)|\.(?:chat|conversation)\.(?:json|jsonl)$|\.chatlog$/i;
const patterns = [
  ['Private key', /-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP )?PRIVATE KEY-----/],
  ['AWS access key', /\b(?:AKIA|ASIA)[0-9A-Z]{16}\b/],
  ['GitHub token', /\bgh[pousr]_[A-Za-z0-9]{36,255}\b/],
  ['GitHub fine-grained token', /\bgithub_pat_[A-Za-z0-9_]{70,255}\b/],
  ['OpenAI API key', /\bsk-(?:proj-)?[A-Za-z0-9_-]{20,}\b/],
  ['Absolute user profile path', /[A-Za-z]:\\Users\\/]
];

const files = execFileSync('git', ['ls-files', '-z']).toString('utf8').split('\0').filter(Boolean);
const findings = [];
for (const file of files) {
  const normalized = file.replaceAll('\\', '/');
  if (forbiddenPath.test(normalized)) {
    findings.push({ file, line: 1, name: 'Forbidden context file' });
    continue;
  }
  const bytes = readFileSync(file);
  if (bytes.includes(0)) continue;
  for (const [index, line] of bytes.toString('utf8').split(/\r?\n/).entries()) {
    for (const [name, pattern] of patterns) if (pattern.test(line)) findings.push({ file, line: index + 1, name });
  }
}
if (findings.length) {
  for (const finding of findings) console.error(`${finding.file}:${finding.line} — ${finding.name}`);
  process.exit(1);
}
console.log('Public tree verified.');
