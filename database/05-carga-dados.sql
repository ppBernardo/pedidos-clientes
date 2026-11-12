-- Carga de dados de exemplo. Só roda com o banco vazio, e é determinística:
-- sempre gera os mesmos dados, para os exemplos do README serem reproduzíveis.
--
-- Datas fixas de jan/2025 a out/2026 (nunca no futuro), para um pedido criado
-- agora sempre aparecer no topo da listagem (CA-15).
--
-- Situações de propósito:
--   * duas clientes "Maria Silva" (Ids 4 e 19), uma delas entre as que mais
--     compram: o GROUP BY c.Nome da query da Parte 4 funde as duas;
--   * clientes sem nenhum pedido (Ids 27 a 30);
--   * produto com estoque baixo (23), sem estoque (24) e inativo (25);
--   * pedidos de 2025 com preço 8% menor que o atual, o que mostra que o item
--     guarda o preço da época (RN-10).
--
-- Tudo num único batch (sem GO), para o RETURN interromper o script inteiro.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    PRINT N'Carga de exemplo já aplicada; nada a fazer.';
    RETURN;
END;

BEGIN TRANSACTION;

SET IDENTITY_INSERT dbo.Clientes ON;
INSERT INTO dbo.Clientes (Id, Nome, Email, DataCadastro) VALUES
    ( 1, N'Ana Souza',         N'ana.souza@exemplo.com.br',          '2024-01-08T13:20:00'),
    ( 2, N'João Pereira',      N'joao.pereira@exemplo.com.br',       '2024-01-19T09:45:00'),
    ( 3, N'Carlos Lima',       N'carlos.lima@exemplo.com.br',        '2024-02-02T16:10:00'),
    ( 4, N'Maria Silva',       N'maria.silva@exemplo.com.br',        '2024-02-14T11:00:00'),
    ( 5, N'Fernanda Costa',    N'fernanda.costa@exemplo.com.br',     '2024-03-01T15:30:00'),
    ( 6, N'Lucas Oliveira',    N'lucas.oliveira@exemplo.com.br',     '2024-03-12T10:05:00'),
    ( 7, N'Juliana Santos',    N'juliana.santos@exemplo.com.br',     '2024-03-27T17:40:00'),
    ( 8, N'Rafael Almeida',    N'rafael.almeida@exemplo.com.br',     '2024-04-09T08:55:00'),
    ( 9, N'Patrícia Rocha',    N'patricia.rocha@exemplo.com.br',     '2024-04-22T14:15:00'),
    (10, N'Bruno Carvalho',    N'bruno.carvalho@exemplo.com.br',     '2024-05-06T12:00:00'),
    (11, N'Camila Ribeiro',    N'camila.ribeiro@exemplo.com.br',     '2024-05-21T09:30:00'),
    (12, N'Diego Martins',     N'diego.martins@exemplo.com.br',      '2024-06-03T18:20:00'),
    (13, N'Larissa Gomes',     N'larissa.gomes@exemplo.com.br',      '2024-06-17T10:45:00'),
    (14, N'Thiago Barbosa',    N'thiago.barbosa@exemplo.com.br',     '2024-07-01T13:35:00'),
    (15, N'Beatriz Araújo',    N'beatriz.araujo@exemplo.com.br',     '2024-07-15T16:50:00'),
    (16, N'Gustavo Mendes',    N'gustavo.mendes@exemplo.com.br',     '2024-07-30T11:25:00'),
    (17, N'Aline Cardoso',     N'aline.cardoso@exemplo.com.br',      '2024-08-12T15:05:00'),
    (18, N'Felipe Teixeira',   N'felipe.teixeira@exemplo.com.br',    '2024-08-26T09:10:00'),
    (19, N'Maria Silva',       N'maria.s.silva@exemplo.com.br',      '2024-09-09T14:40:00'),
    (20, N'Rodrigo Nunes',     N'rodrigo.nunes@exemplo.com.br',      '2024-09-23T17:15:00'),
    (21, N'Mariana Freitas',   N'mariana.freitas@exemplo.com.br',    '2024-10-07T08:30:00'),
    (22, N'Eduardo Moreira',   N'eduardo.moreira@exemplo.com.br',    '2024-10-21T12:55:00'),
    (23, N'Gabriela Dias',     N'gabriela.dias@exemplo.com.br',      '2024-11-04T10:20:00'),
    (24, N'Leonardo Castro',   N'leonardo.castro@exemplo.com.br',    '2024-11-18T16:00:00'),
    (25, N'Isabela Pinto',     N'isabela.pinto@exemplo.com.br',      '2024-12-02T13:45:00'),
    (26, N'André Monteiro',    N'andre.monteiro@exemplo.com.br',     '2024-12-16T09:05:00'),
    (27, N'Natália Azevedo',   N'natalia.azevedo@exemplo.com.br',    '2025-03-10T14:30:00'),
    (28, N'Vinícius Ramos',    N'vinicius.ramos@exemplo.com.br',     '2025-06-23T11:50:00'),
    (29, N'Letícia Moura',     N'leticia.moura@exemplo.com.br',      '2025-09-15T15:20:00'),
    (30, N'Otávio Brandão',    N'otavio.brandao@exemplo.com.br',     '2026-02-02T10:10:00');
SET IDENTITY_INSERT dbo.Clientes OFF;

SET IDENTITY_INSERT dbo.Produtos ON;
INSERT INTO dbo.Produtos (Id, Nome, Preco, Estoque, Ativo) VALUES
    ( 1, N'Notebook ultrafino 14"',          4599.90,  40, 1),
    ( 2, N'Monitor 27" 4K',                   2199.00,  35, 1),
    ( 3, N'Teclado mecânico',                  449.90, 120, 1),
    ( 4, N'Mouse sem fio',                     129.90, 200, 1),
    ( 5, N'Headset com microfone',             349.00,  80, 1),
    ( 6, N'Webcam Full HD',                    299.90,  60, 1),
    ( 7, N'Dock USB-C',                        899.00,  50, 1),
    ( 8, N'SSD NVMe 1 TB',                     599.90, 150, 1),
    ( 9, N'Memória RAM 16 GB',                 389.00, 140, 1),
    (10, N'Cadeira ergonômica',               1899.00,  25, 1),
    (11, N'Mesa com regulagem de altura',     2799.00,  15, 1),
    (12, N'Hub USB 7 portas',                  159.90,  90, 1),
    (13, N'Cabo HDMI 2.1 (2 m)',                79.90, 300, 1),
    (14, N'Suporte articulado para monitor',   249.90,  70, 1),
    (15, N'Roteador Wi-Fi 6',                  699.00,  45, 1),
    (16, N'Impressora multifuncional',        1299.00,  20, 1),
    (17, N'Tablet 11"',                       2499.00,  30, 1),
    (18, N'Caixa de som Bluetooth',            279.90,  85, 1),
    (19, N'Nobreak 1500 VA',                  1199.00,  18, 1),
    (20, N'Pen drive 128 GB',                   69.90, 250, 1),
    (21, N'Mouse pad estendido',                89.90, 180, 1),
    (22, N'Carregador USB-C 65 W',             199.90, 110, 1),
    (23, N'Placa de vídeo RTX',               5499.00,   2, 1), -- estoque baixo
    (24, N'Monitor ultrawide 34"',            3299.00,   0, 1), -- sem estoque
    (25, N'Projetor portátil',                1899.00,  10, 0); -- inativo
SET IDENTITY_INSERT dbo.Produtos OFF;

-- 1.500 pedidos espalhados entre 2025-01-01 e 2026-10-31 23:59:59
-- (669 dias = 57.801.600 segundos). Um terço vai para os clientes 1 a 5, que
-- formam o grupo de quem mais compra; os demais se distribuem entre os
-- clientes 1 a 26.
SET IDENTITY_INSERT dbo.Pedidos ON;
INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido)
SELECT s.value,
       CASE WHEN s.value % 3 = 0 THEN 1 + (s.value / 3) % 5
            ELSE 1 + (s.value * 7) % 26 END,
       DATEADD(SECOND,
               CAST((CAST(s.value AS BIGINT) * 40503) % 57801600 AS INT),
               CAST('2025-01-01T00:00:00' AS DATETIME2(0)))
FROM GENERATE_SERIES(1, 1500) AS s;
SET IDENTITY_INSERT dbo.Pedidos OFF;

-- 1 a 4 itens por pedido, sempre produtos distintos (deslocamentos 0, 7, 14 e
-- 21 módulo 22), só entre os produtos ativos com estoque 1 a 22.
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT p.Id,
       prod.Id,
       1 + (p.Id + j.value) % 3,
       CASE WHEN p.DataPedido < '2026-01-01' THEN ROUND(prod.Preco * 0.92, 2) ELSE prod.Preco END
FROM dbo.Pedidos AS p
CROSS JOIN GENERATE_SERIES(0, 3) AS j
JOIN dbo.Produtos AS prod ON prod.Id = 1 + (p.Id * 3 + j.value * 7) % 22
WHERE j.value <= p.Id % 4;

COMMIT TRANSACTION;

DECLARE @clientes INT = (SELECT COUNT(*) FROM dbo.Clientes),
        @produtos INT = (SELECT COUNT(*) FROM dbo.Produtos),
        @pedidos  INT = (SELECT COUNT(*) FROM dbo.Pedidos),
        @itens    INT = (SELECT COUNT(*) FROM dbo.ItensPedido);
PRINT CONCAT(N'Carga aplicada: ', @clientes, N' clientes, ', @produtos, N' produtos, ',
             @pedidos, N' pedidos, ', @itens, N' itens.');
