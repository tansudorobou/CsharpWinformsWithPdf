import { build } from 'esbuild';
import { cp, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const output = fileURLToPath(new URL('./editor/dist/', import.meta.url));
await mkdir(output, { recursive: true });
await build({
  entryPoints: [fileURLToPath(new URL('./editor/editor.js', import.meta.url))],
  outfile: fileURLToPath(new URL('./editor/dist/editor.js', import.meta.url)),
  bundle: true,
  minify: true,
  format: 'esm',
  platform: 'browser',
  external: ['node:*'],
});
await cp(
  fileURLToPath(new URL('./node_modules/@pdfme/converter/dist/assets/', import.meta.url)),
  fileURLToPath(new URL('./editor/dist/assets/', import.meta.url)),
  { recursive: true, force: true },
);
console.log(`Built pdfme editor and PDF render worker in ${output}`);
