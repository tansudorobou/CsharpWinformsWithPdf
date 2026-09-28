import { Designer } from '@pdfme/ui';
import { text, barcodes } from '@pdfme/schemas';

let designer;
let originalValues = {};
const post = (message) => window.chrome.webview.postMessage(message);

function bytesFromBase64(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index++) bytes[index] = binary.charCodeAt(index);
  return bytes;
}

window.chrome.webview.addEventListener('message', (event) => {
  if (event.data?.type !== 'load') return;
  try {
    const { layout, basePdf, font } = event.data;
    originalValues = Object.fromEntries(layout.fields.map((field) => [field.name, field.value]));
    const schemas = layout.fields.map((field) => ({
      ...(field.pdfmeOptions ?? {}),
      name: field.name,
      type: field.kind,
      position: { x: field.x, y: field.y },
      width: field.width,
      height: field.height,
      content: field.value,
      ...(field.kind === 'text' ? {
        fontSize: field.fontSize,
        alignment: field.alignment ?? 'left',
        verticalAlignment: field.verticalAlignment ?? 'top',
        ...(font ? { fontName: 'ReportFont' } : {}),
      } : {}),
    }));
    const options = { lang: 'ja' };
    if (font) options.font = { ReportFont: { data: bytesFromBase64(font), fallback: true } };
    designer?.destroy();
    designer = new Designer({
      domContainer: document.getElementById('designer'),
      template: {
        basePdf: basePdf ? `data:application/pdf;base64,${basePdf}` : { width: layout.pageWidth, height: layout.pageHeight, padding: [0, 0, 0, 0] },
        schemas: [schemas],
      },
      plugins: { text, qrcode: barcodes.qrcode, code128: barcodes.code128 },
      options,
    });
    document.getElementById('apply').disabled = false;
    post({ type: 'loaded' });
  } catch (error) {
    post({ type: 'error', message: String(error?.message ?? error) });
  }
});

document.getElementById('apply').addEventListener('click', () => {
  try {
    const pages = designer.getTemplate().schemas;
    if (pages.length !== 1) throw new Error('このアプリは1ページの配置に対応しています。ページを1つに戻してください。');
    const fields = pages[0].map((schema) => {
      const {
        id: _id,
        name,
        type: kind,
        position,
        width,
        height,
        fontSize = 12,
        content,
        alignment = 'left',
        verticalAlignment = 'top',
        fontName: _fontName,
        ...pdfmeOptions
      } = schema;
      return {
        name,
        kind,
        x: position.x,
        y: position.y,
        width,
        height,
        fontSize,
        value: content ?? originalValues[name] ?? '',
        alignment,
        verticalAlignment,
        pdfmeOptions: Object.keys(pdfmeOptions).length > 0 ? pdfmeOptions : undefined,
      };
    });
    post({ type: 'apply', fields });
  } catch (error) {
    post({ type: 'error', message: String(error?.message ?? error) });
  }
});

document.getElementById('cancel').addEventListener('click', () => post({ type: 'cancel' }));
post({ type: 'ready' });
