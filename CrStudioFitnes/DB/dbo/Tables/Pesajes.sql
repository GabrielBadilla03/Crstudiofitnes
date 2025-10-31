CREATE TABLE [dbo].[Pesajes] (
    [IdPesaje]    INT            IDENTITY (1, 1) NOT NULL,
    [IdHistorial] INT            NOT NULL,
    [Fecha]       DATE           NOT NULL,
    [Peso]        DECIMAL (6, 2) NOT NULL,
    CONSTRAINT [PK_Pesajes] PRIMARY KEY CLUSTERED ([IdPesaje] ASC),
    CONSTRAINT [FK_Pesajes_Historiales_IdHistorial] FOREIGN KEY ([IdHistorial]) REFERENCES [dbo].[Historiales] ([IdHistorial]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Pesajes_IdHistorial]
    ON [dbo].[Pesajes]([IdHistorial] ASC);

