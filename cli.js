#!/usr/bin/env node
'use strict';
const path = require('path');
const { spawn } = require('child_process');
const pixels = require('./portrait-data.json');
const preview = process.argv.includes('--preview');
const tty = Boolean(process.stdout.isTTY);
const color = tty && !process.env.NO_COLOR;
const ramp = ' .,:;+*#%@';
const reset = color ? '\x1b[0m' : '';
const tint = (r,g,b) => color ? `\x1b[38;2;${r};${g};${b}m` : '';
if (process.platform !== 'win32' && !preview) { console.error('OVERDRIVE needs 64-bit Windows.'); process.exit(1); }
function portrait(frame) {
  const lines = [];
  for (let y = 0; y < 27; y++) {
    let line = '  ';
    for (let x = 0; x < 46; x++) {
      const [r,g,b] = pixels[y * 46 + x];
      const luminance = (r * .2126 + g * .7152 + b * .0722) / 255;
      const shimmer = tty ? Math.sin(x * .9 + y * .4 + frame * .8) * .035 : 0;
      const index = Math.max(0, Math.min(ramp.length - 1, Math.floor((luminance + shimmer) * ramp.length)));
      line += (index ? tint(r,g,b) : '') + ramp[index];
    }
    lines.push(line + reset);
  }
  return lines.join('\n');
}
function draw(frame) {
  const heading = `${tint(168,216,255)}  OVERDRIVE${reset}  /  FREE WINDOWS PROJECT`;
  const footer = preview ? '  PREVIEW ONLY  ·  NOTHING CHANGED' : '  STARTING WINDOWS OPTIMIZER  ·  ADMIN ACCESS REQUIRED';
  return `\n${heading}\n\n${portrait(frame)}\n\n${footer}\n`;
}
function start() {
  if (preview) return;
  console.log('  Old temporary files will be deleted. Windows will ask for admin access.');
  const child = spawn(path.join(__dirname, 'bin', 'Overdrive.exe'), ['--optimize'], { detached: true, stdio: 'ignore', windowsHide: false });
  child.on('error', e => { console.error('Could not start OVERDRIVE: ' + e.message); process.exitCode = 1; });
  child.unref();
}
if (!tty) { process.stdout.write(draw(0)); start(); }
else {
  process.stdout.write('\x1b[?25l' + draw(0));
  let frame = 0;
  const timer = setInterval(() => {
    if (++frame >= 8) { clearInterval(timer); process.stdout.write('\x1b[?25h'); start(); return; }
    process.stdout.write('\x1b[33A\x1b[J' + draw(frame));
  }, 160);
  process.on('SIGINT', () => { clearInterval(timer); process.stdout.write('\x1b[?25h\n'); process.exit(130); });
}