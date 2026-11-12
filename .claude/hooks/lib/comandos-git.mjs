// Análise de comandos de shell (Bash ou PowerShell) para achar comandos git
// que reescrevem ou apagam histórico. Lógica pura, sem I/O, para ser testável.

const OPCOES_GLOBAIS_COM_VALOR = new Set([
  '-C', '-c', '--git-dir', '--work-tree', '--namespace', '--exec-path', '--config-env',
]);
const PREFIXOS_IGNORADOS = new Set(['sudo', 'command', 'exec', 'nohup', 'time', '&']);
const ATRIBUICAO_DE_VARIAVEL = /^[A-Za-z_][A-Za-z0-9_]*=/;
const REF_RELATIVA = /^(HEAD|@)([~^]\d*)+$/;
const SHA = /^[0-9a-f]{7,40}$/i;

/**
 * Remove o corpo de heredocs (bash) e here-strings (PowerShell) para que o texto
 * de uma mensagem de commit não seja confundido com um comando.
 */
export function removerTextoLiteral(comando) {
  const linhas = comando.split(/\r?\n/);
  const saida = [];
  let fimHeredoc = null;
  let fimHereString = null;

  for (const linha of linhas) {
    if (fimHeredoc !== null) {
      if (linha.trim() === fimHeredoc) fimHeredoc = null;
      continue;
    }
    if (fimHereString !== null) {
      if (linha.trimStart().startsWith(fimHereString)) fimHereString = null;
      continue;
    }
    saida.push(linha);
    const heredoc = linha.match(/<<-?\s*(['"]?)([A-Za-z_][A-Za-z0-9_]*)\1/);
    if (heredoc) fimHeredoc = heredoc[2];
    const hereString = linha.match(/@(['"])\s*$/);
    if (hereString) fimHereString = `${hereString[1]}@`;
  }
  return saida.join('\n');
}

/** Quebra em comandos simples (&&, ||, ;, |, &, quebra de linha), respeitando aspas. */
export function separarComandos(texto) {
  const comandos = [];
  let atual = '';
  let aspas = null;

  for (let i = 0; i < texto.length; i++) {
    const c = texto[i];
    if (aspas) {
      atual += c;
      if (c === aspas) aspas = null;
      continue;
    }
    if (c === '"' || c === "'") {
      aspas = c;
      atual += c;
      continue;
    }
    if (c === ';' || c === '|' || c === '&' || c === '\n') {
      comandos.push(atual);
      atual = '';
      continue;
    }
    atual += c;
  }
  comandos.push(atual);
  return comandos.map((c) => c.trim()).filter(Boolean);
}

/** Separa um comando simples em palavras, tirando as aspas. */
export function separarPalavras(comando) {
  const palavras = [];
  let atual = '';
  let aspas = null;
  let temPalavra = false;

  for (const c of comando) {
    if (aspas) {
      if (c === aspas) aspas = null;
      else atual += c;
      continue;
    }
    if (c === '"' || c === "'") {
      aspas = c;
      temPalavra = true;
      continue;
    }
    if (/\s/.test(c)) {
      if (temPalavra) palavras.push(atual);
      atual = '';
      temPalavra = false;
      continue;
    }
    atual += c;
    temPalavra = true;
  }
  if (temPalavra) palavras.push(atual);
  return palavras;
}

function ehExecutavelGit(palavra) {
  const nome = palavra.split(/[\\/]/).pop().toLowerCase();
  return nome === 'git' || nome === 'git.exe';
}

/** Devolve { subcomando, args } se o comando simples for uma chamada ao git. */
export function interpretarGit(palavras) {
  let i = 0;
  while (i < palavras.length && (PREFIXOS_IGNORADOS.has(palavras[i]) || ATRIBUICAO_DE_VARIAVEL.test(palavras[i]))) {
    i++;
  }
  if (i >= palavras.length || !ehExecutavelGit(palavras[i])) return null;
  i++;

  while (i < palavras.length && palavras[i].startsWith('-')) {
    const opcao = palavras[i];
    i += OPCOES_GLOBAIS_COM_VALOR.has(opcao) ? 2 : 1;
  }
  if (i >= palavras.length) return null;
  return { subcomando: palavras[i], args: palavras.slice(i + 1) };
}

const temOpcao = (args, ...opcoes) =>
  args.some((a) => opcoes.some((o) => a === o || a.startsWith(`${o}=`)));
const temOpcaoCurta = (args, letra) =>
  args.some((a) => /^-[A-Za-z]+$/.test(a) && a.slice(1).includes(letra));

const REVERTER = 'Para desfazer um commit, use `git revert <commit>`; para corrigir, faça um novo commit (ex.: `fix: ...`).';

const regras = {
  rebase: (args) =>
    temOpcao(args, '--abort', '--quit')
      ? null
      : `\`git rebase\` reescreve commits. Integre com \`git merge\` ou faça novos commits. ${REVERTER}`,

  commit: (args) =>
    temOpcao(args, '--amend') ? `\`git commit --amend\` reescreve o último commit. ${REVERTER}` : null,

  push: (args) => {
    if (temOpcao(args, '--force', '--force-with-lease', '--force-if-includes') || temOpcaoCurta(args, 'f')) {
      return 'Push forçado sobrescreve o histórico remoto. Faça push apenas de commits novos.';
    }
    if (args.some((a) => a.startsWith('+'))) {
      return 'Refspec com `+` força a atualização do remoto. Faça push apenas de commits novos.';
    }
    if (temOpcao(args, '--delete', '--mirror', '--prune') || temOpcaoCurta(args, 'd') || args.some((a) => a.startsWith(':'))) {
      return 'Este push apaga ou espelha refs remotas. Remova branches pela interface do GitHub, se for mesmo necessário.';
    }
    return null;
  },

  reset: (args) => {
    if (temOpcao(args, '--hard', '--soft', '--merge', '--keep')) {
      return `\`git reset\` com --hard/--soft/--merge/--keep move o branch e descarta histórico. ${REVERTER}`;
    }
    const alvo = args.find((a) => !a.startsWith('-'));
    if (alvo && args.indexOf('--') !== -1 && args.indexOf(alvo) > args.indexOf('--')) return null;
    if (alvo && (REF_RELATIVA.test(alvo) || SHA.test(alvo))) {
      return `\`git reset ${alvo}\` move o branch para outro commit. ${REVERTER} Para tirar arquivos do stage: \`git restore --staged <arquivo>\`.`;
    }
    return null;
  },

  branch: (args) =>
    temOpcao(args, '--force') || temOpcaoCurta(args, 'f')
      ? '`git branch -f` move um branch existente e pode descartar commits.'
      : null,

  checkout: (args) => (temOpcaoCurta(args, 'B') ? '`git checkout -B` redefine um branch existente.' : null),
  switch: (args) =>
    temOpcao(args, '--force-create') || temOpcaoCurta(args, 'C') ? '`git switch -C` redefine um branch existente.' : null,

  'filter-branch': () => '`git filter-branch` reescreve todo o histórico.',
  'filter-repo': () => '`git filter-repo` reescreve todo o histórico.',
  'update-ref': () => '`git update-ref` altera refs diretamente e pode descartar commits.',
};

/** Devolve o motivo do bloqueio, ou null se o comando for permitido. */
export function motivoDeBloqueio(comando) {
  if (typeof comando !== 'string' || comando.trim() === '') return null;

  for (const simples of separarComandos(removerTextoLiteral(comando))) {
    const git = interpretarGit(separarPalavras(simples));
    if (!git) continue;
    const regra = regras[git.subcomando];
    const motivo = regra ? regra(git.args) : null;
    if (motivo) return motivo;
  }
  return null;
}
