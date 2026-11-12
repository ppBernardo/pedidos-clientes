-- Tabelas, chaves e constraints. Idempotente.
-- O banco vem do sqlcmd (-d), para o mesmo script servir ao banco da Parte 4.
-- Os índices de suporte ficam em 03-indices.sql.

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id           INT IDENTITY(1, 1) NOT NULL,
        Nome         NVARCHAR(150)      NOT NULL, -- não é único: existem homônimos
        Email        NVARCHAR(254)      NOT NULL,
        DataCadastro DATETIME2(0)       NOT NULL CONSTRAINT DF_Clientes_DataCadastro DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UX_Clientes_Email UNIQUE (Email)
    );
END;
GO

IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id      INT IDENTITY(1, 1) NOT NULL,
        Nome    NVARCHAR(150)      NOT NULL,
        Preco   DECIMAL(18, 2)     NOT NULL,
        Estoque INT                NOT NULL,
        Ativo   BIT                NOT NULL CONSTRAINT DF_Produtos_Ativo DEFAULT (1),

        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Produtos_Preco CHECK (Preco >= 0),
        -- Última barreira contra estoque negativo. A API já faz a baixa condicional (RN-09).
        CONSTRAINT CK_Produtos_Estoque CHECK (Estoque >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos
    (
        Id         INT IDENTITY(1, 1) NOT NULL,
        ClienteId  INT                NOT NULL,
        DataPedido DATETIME2(0)       NOT NULL, -- UTC, definido pela API (RN-11)

        CONSTRAINT PK_Pedidos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id)
    );
END;
GO

-- O valor total do pedido não é armazenado: a fonte da verdade são os itens.
IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido
    (
        Id            INT IDENTITY(1, 1) NOT NULL,
        PedidoId      INT                NOT NULL,
        ProdutoId     INT                NOT NULL,
        Quantidade    INT                NOT NULL,
        PrecoUnitario DECIMAL(18, 2)     NOT NULL, -- preço do produto no momento do pedido (RN-10)

        CONSTRAINT PK_ItensPedido PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id),
        CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0)
    );
END;
GO
