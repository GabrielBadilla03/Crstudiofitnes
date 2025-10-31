CREATE TABLE [dbo].[PagosPaqueteDetalle] (
    [IdPagoPaqueteDetalle] INT             IDENTITY (1, 1) NOT NULL,
    [IdPagoPaquete]        INT             NOT NULL,
    [CantDias]             NVARCHAR (MAX)  NOT NULL,
    [CantLecciones]        INT             NULL,
    [Pago]                 DECIMAL (10, 2) NOT NULL,
    [Detalle]              NVARCHAR (200)  NULL,
    CONSTRAINT [PK_PagosPaqueteDetalle] PRIMARY KEY CLUSTERED ([IdPagoPaqueteDetalle] ASC),
    CONSTRAINT [FK_PagosPaqueteDetalle_PagosPaquete_IdPagoPaquete] FOREIGN KEY ([IdPagoPaquete]) REFERENCES [dbo].[PagosPaquete] ([IdPagoPaquete]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_PagosPaqueteDetalle_IdPagoPaquete]
    ON [dbo].[PagosPaqueteDetalle]([IdPagoPaquete] ASC);

