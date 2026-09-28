import { readFile, writeFile } from 'node:fs/promises';
import { generate } from '@pdfme/generator';
import { text, barcodes } from '@pdfme/schemas';

const [, , jobPath, outputPath] = process.argv;
if (!jobPath || !outputPath) {
  console.error('Usage: node runner.mjs <job.json> <output.pdf>');
  process.exit(2);
}

try {
  const job = JSON.parse(await readFile(jobPath, 'utf8'));
  const basePdf = job.basePdfPath
    ? new Uint8Array(await readFile(job.basePdfPath))
    : { width: job.pageWidth ?? 210, height: job.pageHeight ?? 297, padding: [0, 0, 0, 0] };
  const options = {};
  if (job.fontPath) {
    options.font = { ReportFont: { data: new Uint8Array(await readFile(job.fontPath)), fallback: true } };
  }
  for (const key of ['title', 'author', 'subject', 'lang', 'creator', 'producer']) {
    if (job.metadata?.[key]) options[key] = job.metadata[key];
  }
  if (job.metadata?.keywords?.length) options.keywords = job.metadata.keywords;
  const pdf = await generate({
    template: { basePdf, schemas: job.template.schemas },
    inputs: job.inputs,
    plugins: { text, qrcode: barcodes.qrcode, code128: barcodes.code128 },
    options: Object.keys(options).length > 0 ? options : undefined,
  });
  await writeFile(outputPath, pdf);
  console.log(outputPath);
} catch (error) {
  console.error(error instanceof Error ? error.stack : String(error));
  process.exitCode = 1;
}
