// Hook PostToolUse (Write|Edit): roda a verificação rápida da área editada e
// devolve os erros para o Claude corrigir na hora.
//   backend/**/*.cs, *.csproj     -> dotnet build do projeto mais próximo
//   backend/*.props, *.slnx, ...  -> dotnet build da solução
//   frontend/**/*.ts(x)           -> tsc -b (typecheck) + oxlint no arquivo
// Saída 2 + stderr: o Claude recebe os erros. Tudo certo: saída 0 em silêncio.
// Para desligar temporariamente (ex.: refatoração grande): PEDIDOS_HOOK_VERIFICACAO=0.
import { spawnSync } from 'node:child_process';
import { closeSync, existsSync, openSync, readFileSync, rmSync, statSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { classificar, ehErroDeArquivoTravado, resumirErros } from './lib/verificacao.mjs';

if (process.env.PEDIDOS_HOOK_VERIFICACAO === '0') process.exit(0);

let entrada;
try {
  entrada = JSON.parse(readFileSync(0, 'utf8'));
} catch {
  process.exit(0);
}

const raiz = process.env.CLAUDE_PROJECT_DIR || entrada.cwd || process.cwd();
const acao = classificar(entrada?.tool_input?.file_path, raiz);
if (!acao) process.exit(0);

// Edições em paralelo disparam vários hooks ao mesmo tempo; builds simultâneos
// do mesmo projeto brigam por bin/obj. Um lock serializa as verificações.
const lock = path.join(tmpdir(), `pedidos-hook-${createHash('sha1').update(raiz).digest('hex').slice(0, 12)}.lock`);
const LOCK_EXPIRADO_MS = 5 * 60_000;

async function comLock(fn) {
  const limite = Date.now() + 150_000;
  for (;;) {
    try {
      closeSync(openSync(lock, 'wx'));
      break;
    } catch {
      try {
        if (Date.now() - statSync(lock).mtimeMs > LOCK_EXPIRADO_MS) rmSync(lock, { force: true });
      } catch {
        /* lock sumiu entre as chamadas */
      }
      if (Date.now() > limite) return null; // outra verificação ainda rodando: ela reporta
      await new Promise((r) => setTimeout(r, 500));
    }
  }
  try {
    return fn();
  } finally {
    rmSync(lock, { force: true });
  }
}

function rodar(comando, args, cwd) {
  const r = spawnSync(comando, args, { cwd, encoding: 'utf8', timeout: 170_000, windowsHide: true });
  return { ok: r.status === 0, saida: `${r.stdout ?? ''}\n${r.stderr ?? ''}`, erro: r.error };
}

function verificarDotnet(alvo) {
  const r = rodar('dotnet', ['build', alvo, '--nologo', '-v', 'q', '-clp:NoSummary;ErrorsOnly', '-nodeReuse:false'], raiz);
  if (r.erro) return null; // SDK ausente: não bloqueia o trabalho
  if (r.ok || ehErroDeArquivoTravado(r.saida)) return null;
  return `dotnet build falhou em ${path.relative(raiz, alvo)}:\n${resumirErros(r.saida)}`;
}

function verificarFrontend(arquivo) {
  const pasta = path.join(raiz, 'frontend');
  const modulos = path.join(pasta, 'node_modules');
  if (!existsSync(modulos)) return null; // dependências não instaladas: nada a verificar

  const problemas = [];
  const tsc = rodar(process.execPath, [path.join(modulos, 'typescript', 'bin', 'tsc'), '-b'], pasta);
  if (!tsc.ok) problemas.push(`tsc -b:\n${resumirErros(tsc.saida)}`);

  const lint = rodar(process.execPath, [path.join(modulos, 'oxlint', 'bin', 'oxlint'), '--deny-warnings', arquivo], pasta);
  if (!lint.ok) problemas.push(`oxlint ${arquivo}:\n${resumirErros(lint.saida)}`);

  return problemas.length > 0 ? problemas.join('\n\n') : null;
}

const problema = await comLock(() => (acao.tipo === 'dotnet' ? verificarDotnet(acao.alvo) : verificarFrontend(acao.arquivo)));
if (problema) {
  process.stderr.write(`Verificação pós-edição encontrou problemas. Corrija antes de seguir:\n${problema}\n`);
  process.exit(2);
}
process.exit(0);
