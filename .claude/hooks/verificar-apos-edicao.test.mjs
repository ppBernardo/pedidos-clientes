import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { candidatosDotnet, classificar, ehErroDeArquivoTravado, ehSdkAusente, resumirErros } from './lib/verificacao.mjs';

function criarProjetoFalso() {
  const raiz = mkdtempSync(path.join(tmpdir(), 'pedidos-hook-'));
  const api = path.join(raiz, 'backend', 'src', 'Pedidos.Api');
  mkdirSync(path.join(api, 'Endpoints'), { recursive: true });
  writeFileSync(path.join(api, 'Pedidos.Api.csproj'), '<Project />');
  mkdirSync(path.join(raiz, 'frontend', 'src'), { recursive: true });
  return { raiz, api };
}

test('arquivo .cs aponta para o .csproj mais próximo', () => {
  const { raiz, api } = criarProjetoFalso();
  const acao = classificar(path.join(api, 'Endpoints', 'PedidoEndpoints.cs'), raiz);
  assert.deepEqual(acao, { tipo: 'dotnet', alvo: path.join(api, 'Pedidos.Api.csproj') });
});

test('arquivo de build do backend aponta para a solução', () => {
  const { raiz } = criarProjetoFalso();
  const acao = classificar(path.join(raiz, 'backend', 'Directory.Packages.props'), raiz);
  assert.deepEqual(acao, { tipo: 'dotnet', alvo: path.join(raiz, 'backend', 'Pedidos.slnx') });
});

test('global.json da raiz aponta para a solução', () => {
  const { raiz } = criarProjetoFalso();
  const acao = classificar(path.join(raiz, 'global.json'), raiz);
  assert.deepEqual(acao, { tipo: 'dotnet', alvo: path.join(raiz, 'backend', 'Pedidos.slnx') });
});

test('arquivo .tsx do frontend vira verificação do frontend', () => {
  const { raiz } = criarProjetoFalso();
  const acao = classificar(path.join(raiz, 'frontend', 'src', 'App.tsx'), raiz);
  assert.deepEqual(acao, { tipo: 'frontend', arquivo: 'src/App.tsx' });
});

test('caminho relativo também é aceito', () => {
  const { raiz } = criarProjetoFalso();
  assert.equal(classificar('frontend/src/App.tsx', raiz)?.tipo, 'frontend');
});

test('arquivos que não pedem verificação são ignorados', () => {
  const { raiz } = criarProjetoFalso();
  for (const arquivo of ['README.md', 'docs/spec.md', 'database/02-tabelas.sql', 'frontend/src/styles.css', 'frontend/node_modules/x/index.ts']) {
    assert.equal(classificar(path.join(raiz, arquivo), raiz), null, arquivo);
  }
});

test('arquivo fora do projeto é ignorado', () => {
  const { raiz } = criarProjetoFalso();
  assert.equal(classificar(path.join(tmpdir(), 'outro', 'Program.cs'), raiz), null);
});

test('erro de arquivo travado não é tratado como erro de código', () => {
  assert.ok(ehErroDeArquivoTravado('error MSB3027: Could not copy "obj/x.dll" to "bin/x.dll".'));
  assert.ok(!ehErroDeArquivoTravado("error CS1002: ; expected"));
});

test('SDK ausente é reconhecido e não confundido com erro de código', () => {
  assert.ok(ehSdkAusente('A compatible .NET SDK was not found. Requested SDK version: 10.0.100'));
  assert.ok(ehSdkAusente('error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0.'));
  assert.ok(!ehSdkAusente("Program.cs(3,1): error CS1002: ; expected"));
});

test('tenta o dotnet do PATH e depois a instalação por usuário do dotnet-install', () => {
  assert.deepEqual(candidatosDotnet('win32', { LOCALAPPDATA: 'C:\\Users\\dev\\AppData\\Local' }, 'C:\\Users\\dev'), [
    'dotnet',
    path.join('C:\\Users\\dev\\AppData\\Local', 'Microsoft', 'dotnet', 'dotnet.exe'),
  ]);
  assert.deepEqual(candidatosDotnet('linux', {}, '/home/dev'), ['dotnet', path.join('/home/dev', '.dotnet', 'dotnet')]);
});

test('resumo mantém só as linhas de erro, sem repetição', () => {
  const saida = ['Restore complete', "Program.cs(3,1): error CS1002: ; expected", "Program.cs(3,1): error CS1002: ; expected", 'Build FAILED.'].join('\n');
  assert.equal(resumirErros(saida), "Program.cs(3,1): error CS1002: ; expected");
});
