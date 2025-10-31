CREATE TABLE [dbo].[PagosPaquete] (
    [IdPagoPaquete] INT             IDENTITY (1, 1) NOT NULL,
    [IdUsuario]     NVARCHAR (450)  NOT NULL,
    [Fecha]         DATETIME2 (7)   NOT NULL,
    [Monto]         DECIMAL (10, 2) NOT NULL,
    CONSTRAINT [PK_PagosPaquete] PRIMARY KEY CLUSTERED ([IdPagoPaquete] ASC),
    CONSTRAINT [FK_PagosPaquete_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_PagosPaquete_IdUsuario]
    ON [dbo].[PagosPaquete]([IdUsuario] ASC);

