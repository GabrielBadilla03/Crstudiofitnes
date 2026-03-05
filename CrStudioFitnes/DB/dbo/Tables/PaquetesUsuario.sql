CREATE TABLE [dbo].[PaquetesUsuario] (
    [IdPaqueteUsuario] INT            IDENTITY (1, 1) NOT NULL,
    [IdPaquete]        INT            NOT NULL,
    [IdUsuario]        NVARCHAR (450) NOT NULL,
    [CantLecciones]    INT            NOT NULL,
    [FechaInicio]      DATE           NOT NULL,
    [FechaFin]         DATE           DEFAULT ('0001-01-01') NOT NULL,
    CONSTRAINT [PK_PaquetesUsuario] PRIMARY KEY CLUSTERED ([IdPaqueteUsuario] ASC),
    CONSTRAINT [FK_PaquetesUsuario_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PaquetesUsuario_Paquetes_IdPaquete] FOREIGN KEY ([IdPaquete]) REFERENCES [dbo].[Paquetes] ([IdPaquete])
);




GO
CREATE NONCLUSTERED INDEX [IX_PaquetesUsuario_IdUsuario]
    ON [dbo].[PaquetesUsuario]([IdUsuario] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_PaquetesUsuario_IdPaquete]
    ON [dbo].[PaquetesUsuario]([IdPaquete] ASC);

