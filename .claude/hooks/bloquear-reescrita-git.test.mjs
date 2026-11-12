import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { motivoDeBloqueio } from './lib/comandos-git.mjs';

const BLOQUEADOS = [
  'git commit --amend --no-edit',
  'git commit -m "msg" --amend',
  'git rebase -i HEAD~3',
  'git rebase main',
  'git push --force',
  'git push -f origin main',
  'git push -uf origin main',
  'git push --force-with-lease origin main',
  'git push origin +main',
  'git push origin --delete feature',
  'git push origin :feature',
  'git reset --hard HEAD~1',
  'git reset --soft HEAD~2',
  'git reset HEAD~1',
  'git reset abc1234',
  'git filter-branch --tree-filter "rm x" HEAD',
  'git branch -f main abc1234',
  'git checkout -B main',
  'git -C /repo commit --amend',
  'git -c core.editor=true rebase main',
  'cd /repo && git push -f',
  'GIT_TERMINAL_PROMPT=0 git push --force',
  'git status; git rebase --continue',
  '& git push --force',
  'git.exe commit --amend',
  '"C:\\Program Files\\Git\\cmd\\git.exe" push -f',
];

const PERMITIDOS = [
  'git status',
  'git log --oneline',
  'git commit -m "docs: explica por que não usamos git rebase"',
  "git commit -F - <<'EOF'\nfix: evita git push --force no CI\ngit reset --hard não é usado\nEOF",
  "$msg = @'\nfix: nada de git rebase aqui\n'@\ngit commit -m $msg",
  'git push origin main',
  'git push -u origin main',
  'git rebase --abort',
  'git reset',
  'git reset -- arquivo.txt',
  'git reset HEAD arquivo.txt',
  'git restore --staged arquivo.txt',
  'git revert abc1234',
  'git commit --fixup abc1234',
  'git checkout -b nova-feature',
  'echo git push --force',
  'dotnet test backend/Pedidos.slnx',
  '',
];

for (const comando of BLOQUEADOS) {
  test(`bloqueia: ${comando}`, () => {
    assert.ok(motivoDeBloqueio(comando), `deveria bloquear: ${comando}`);
  });
}

for (const comando of PERMITIDOS) {
  test(`permite: ${JSON.stringify(comando)}`, () => {
    assert.equal(motivoDeBloqueio(comando), null);
  });
}

const HOOK = fileURLToPath(new URL('./bloquear-reescrita-git.mjs', import.meta.url));
const executarHook = (entrada) =>
  spawnSync(process.execPath, [HOOK], { input: JSON.stringify(entrada), encoding: 'utf8' });

test('hook sai com código 2 e explica o motivo no stderr', () => {
  const r = executarHook({ tool_name: 'PowerShell', tool_input: { command: 'git push --force' } });
  assert.equal(r.status, 2);
  assert.match(r.stderr, /Bloqueado pelo hook bloquear-reescrita-git/);
});

test('hook sai com código 0 para comando permitido', () => {
  const r = executarHook({ tool_name: 'Bash', tool_input: { command: 'git status' } });
  assert.equal(r.status, 0);
  assert.equal(r.stderr, '');
});

test('hook não bloqueia quando a entrada não é JSON', () => {
  const r = spawnSync(process.execPath, [HOOK], { input: 'não é json', encoding: 'utf8' });
  assert.equal(r.status, 0);
});
