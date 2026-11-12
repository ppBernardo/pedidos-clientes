-- Executa a consulta $(ARQUIVO) com STATISTICS IO e TIME ligados.
-- As estatísticas saem como mensagens (rode o sqlcmd com -r1 para mandá-las ao stderr).
SET NOCOUNT ON;
SET STATISTICS IO, TIME ON;
:r $(ARQUIVO)
SET STATISTICS IO, TIME OFF;
