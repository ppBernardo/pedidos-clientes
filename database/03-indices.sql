-- Índices de suporte. Idempotente.
-- Ficam separados das tabelas para a Parte 4 medir as consultas com e sem eles.
-- Lembrete: o SQL Server NÃO cria índice para FK automaticamente.

-- FK Pedidos -> Clientes + listagem filtrada por cliente e período.
-- A chave clusterizada (Id) entra na chave do índice, o que também atende o
-- ORDER BY DataPedido DESC, Id DESC.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Pedidos') AND name = N'IX_Pedidos_ClienteId_DataPedido')
    CREATE INDEX IX_Pedidos_ClienteId_DataPedido ON dbo.Pedidos (ClienteId, DataPedido);
GO

-- Listagem só por período e relatório de top clientes: seek no intervalo de
-- datas, com ClienteId incluído para não precisar de lookup.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Pedidos') AND name = N'IX_Pedidos_DataPedido')
    CREATE INDEX IX_Pedidos_DataPedido ON dbo.Pedidos (DataPedido) INCLUDE (ClienteId);
GO

-- FK ItensPedido -> Pedidos + RN-07 (produto não se repete no pedido) garantida
-- no banco. O INCLUDE cobre a soma Quantidade x PrecoUnitario por pedido sem
-- tocar na tabela.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItensPedido') AND name = N'UX_ItensPedido_PedidoId_ProdutoId')
    CREATE UNIQUE INDEX UX_ItensPedido_PedidoId_ProdutoId
        ON dbo.ItensPedido (PedidoId, ProdutoId)
        INCLUDE (Quantidade, PrecoUnitario);
GO

-- FK ItensPedido -> Produtos.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItensPedido') AND name = N'IX_ItensPedido_ProdutoId')
    CREATE INDEX IX_ItensPedido_ProdutoId ON dbo.ItensPedido (ProdutoId);
GO
