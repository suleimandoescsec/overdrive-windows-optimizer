#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');
const portrait = fs.readFileSync(path.join(__dirname, 'portrait.txt'), 'utf8');
const preview = process.argv.includes('--preview');
if (process.platform !== 'win32' && !preview) { console.error('OVERDRIVE needs 64-bit Windows.'); process.exit(1); }
console.log('\n  OVERDRIVE / WINDOWS PERFORMANCE\n');
console.log(portrait);
console.log('  One button. Your settings, startup and old temp files.');
if (preview) { console.log('  Preview only. Nothing changed.'); process.exit(0); }
console.log('  Windows will ask for administrator access. Old temp files will be deleted.');
const exe = path.join(__dirname, 'bin', 'Overdrive.exe');
let frame = 0;
const timer = setInterval(() => {
  if (process.stdout.isTTY) process.stdout.write('\r  ' + ['·    ', '··   ', '···  ', '···· '][frame % 4] + ' Starting OVERDRIVE');
  if (++frame >= 8) {
    clearInterval(timer); if (process.stdout.isTTY) process.stdout.write('\n');
    const child = spawn(exe, ['--optimize'], { detached: true, stdio: 'ignore', windowsHide: false });
    child.on('error', e => { console.error('Could not start OVERDRIVE: ' + e.message); process.exitCode = 1; });
    child.unref();
  }
}, 140);
