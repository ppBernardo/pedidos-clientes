-- Executa a consulta $(ARQUIVO) devolvendo o plano de execução REAL (actual) em XML.
SET NOCOUNT ON;
SET STATISTICS XML ON;
:r $(ARQUIVO)
SET STATISTICS XML OFF;
