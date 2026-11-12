// Hook PreToolUse (Bash|PowerShell): bloqueia comandos git que reescrevem ou
// apagam histórico (rebase, commit --amend, push --force, reset --hard...).
//
// Motivo: o desafio exige histórico de commits preservado, sem squash e sem
// reescrita. Saída 2 + mensagem no stderr faz o Claude Code cancelar a chamada
// e mostrar o motivo ao Claude, que então segue por outro caminho (novo commit).
import { readFileSync } from 'node:fs';
import { motivoDeBloqueio } from './lib/comandos-git.mjs';

let entrada;
try {
  entrada = JSON.parse(readFileSync(0, 'utf8'));
} catch {
  process.exit(0); // entrada inesperada: não bloqueia
}

const motivo = motivoDeBloqueio(entrada?.tool_input?.command);
if (motivo) {
  process.stderr.write(
    `Bloqueado pelo hook bloquear-reescrita-git: ${motivo}\n` +
      'Este projeto preserva o histórico de commits (exigência do desafio).\n',
  );
  process.exit(2);
}
process.exit(0);
