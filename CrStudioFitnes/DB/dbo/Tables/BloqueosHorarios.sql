CREATE TABLE [dbo].[BloqueosHorarios] (
    [IdBloqueoHorario] INT            IDENTITY (1, 1) NOT NULL,
    [Fecha]            DATE           NULL,
    [IdHora]           INT            NULL,
    [Motivo]           NVARCHAR (200) NULL,
    [Activo]           BIT            NOT NULL,
    CONSTRAINT [PK_BloqueosHorarios] PRIMARY KEY CLUSTERED ([IdBloqueoHorario] ASC),
    CONSTRAINT [CK_BloqueosHorarios_FechaOrHora] CHECK ([Fecha] IS NOT NULL OR [IdHora] IS NOT NULL),
    CONSTRAINT [FK_BloqueosHorarios_HorasReserva_IdHora] FOREIGN KEY ([IdHora]) REFERENCES [dbo].[HorasReserva] ([IdHora])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_BloqueosHorarios_IdHora]
    ON [dbo].[BloqueosHorarios]([IdHora] ASC) WHERE ([Activo]=(1) AND [Fecha] IS NULL AND [IdHora] IS NOT NULL);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_BloqueosHorarios_Fecha_IdHora]
    ON [dbo].[BloqueosHorarios]([Fecha] ASC, [IdHora] ASC) WHERE ([Activo]=(1) AND [Fecha] IS NOT NULL AND [IdHora] IS NOT NULL);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_BloqueosHorarios_Fecha]
    ON [dbo].[BloqueosHorarios]([Fecha] ASC) WHERE ([Activo]=(1) AND [Fecha] IS NOT NULL AND [IdHora] IS NULL);

