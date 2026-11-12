// Decide qual verificação rodar para um arquivo editado. Lógica pura, testável.
import { existsSync, readdirSync } from 'node:fs';
import path from 'node:path';

/**
 * @returns {{ tipo: 'dotnet', alvo: string } | { tipo: 'frontend', arquivo: string } | null}
 *   dotnet: alvo = .csproj mais próximo, ou a solução quando o arquivo é de build;
 *   frontend: arquivo relativo à pasta frontend/.
 */
export function classificar(arquivo, raiz) {
  if (!arquivo) return null;
  const relativo = path.relative(raiz, path.resolve(raiz, arquivo));
  const partes = relativo.split(path.sep);
  if (partes[0] === '..' || path.isAbsolute(relativo)) return null; // fora do projeto

  if (partes[0] === 'backend') {
    if (/\.(cs|csproj)$/i.test(relativo)) {
      const projeto = csprojMaisProximo(path.dirname(path.join(raiz, relativo)), path.join(raiz, 'backend'));
      if (projeto) return { tipo: 'dotnet', alvo: projeto };
    }
    if (/\.(props|targets|slnx)$/i.test(relativo) || path.basename(relativo) === 'global.json') {
      return { tipo: 'dotnet', alvo: path.join(raiz, 'backend', 'Pedidos.slnx') };
    }
    return null;
  }

  if (partes[0] === 'frontend' && partes[1] !== 'node_modules' && /\.(ts|tsx)$/i.test(relativo)) {
    return { tipo: 'frontend', arquivo: partes.slice(1).join('/') };
  }
  return null;
}

/** Sobe a partir de `dir` até `limite` procurando um .csproj. */
export function csprojMaisProximo(dir, limite) {
  let atual = dir;
  while (atual.startsWith(limite)) {
    if (existsSync(atual)) {
      const csproj = readdirSync(atual).find((nome) => nome.endsWith('.csproj'));
      if (csproj) return path.join(atual, csproj);
    }
    const pai = path.dirname(atual);
    if (pai === atual) break;
    atual = pai;
  }
  return null;
}

/** Arquivo travado por outro processo (ex.: `dotnet run` aberto): não é erro do código. */
export function ehErroDeArquivoTravado(saida) {
  return /MSB3027|MSB3021|being used by another process|está sendo usado por outro processo/i.test(saida);
}

/** Mantém só as linhas de erro e limita o tamanho, para não inundar o contexto. */
export function resumirErros(saida, maxLinhas = 40) {
  const linhas = saida
    .split(/\r?\n/)
    .map((l) => l.trimEnd())
    .filter((l) => /error|erro|\berr\b|×|✗/i.test(l));
  const unicas = [...new Set(linhas.length > 0 ? linhas : saida.split(/\r?\n/).filter(Boolean))];
  const cortadas = unicas.slice(0, maxLinhas);
  if (unicas.length > maxLinhas) cortadas.push(`... (${unicas.length - maxLinhas} linhas omitidas)`);
  return cortadas.join('\n');
}
