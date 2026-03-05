CREATE TABLE [dbo].[Reservas] (
    [IdReserva] INT            IDENTITY (1, 1) NOT NULL,
    [IdUsuario] NVARCHAR (450) NOT NULL,
    [Fecha]     DATE           NOT NULL,
    [IdHora]    INT            NOT NULL,
    CONSTRAINT [PK_Reservas] PRIMARY KEY CLUSTERED ([IdReserva] ASC),
    CONSTRAINT [FK_Reservas_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reservas_HorasReserva_IdHora] FOREIGN KEY ([IdHora]) REFERENCES [dbo].[HorasReserva] ([IdHora])
);




GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Reservas_IdUsuario_Fecha_IdHora]
    ON [dbo].[Reservas]([IdUsuario] ASC, [Fecha] ASC, [IdHora] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Reservas_IdHora]
    ON [dbo].[Reservas]([IdHora] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Reservas_Fecha_IdHora]
    ON [dbo].[Reservas]([Fecha] ASC, [IdHora] ASC);

