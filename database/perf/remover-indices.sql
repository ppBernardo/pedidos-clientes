-- Parte 4: volta o banco de performance ao estado "só tabelas" (sem os índices
-- de 03-indices.sql), para a medição sem índices ser repetível.
DROP INDEX IF EXISTS IX_Pedidos_ClienteId_DataPedido ON dbo.Pedidos;
DROP INDEX IF EXISTS IX_Pedidos_DataPedido ON dbo.Pedidos;
DROP INDEX IF EXISTS UX_ItensPedido_PedidoId_ProdutoId ON dbo.ItensPedido;
DROP INDEX IF EXISTS IX_ItensPedido_ProdutoId ON dbo.ItensPedido;
