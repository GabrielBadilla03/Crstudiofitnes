CREATE TABLE [dbo].[HorasReserva] (
    [IdHora]   INT           IDENTITY (1, 1) NOT NULL,
    [Hora]     TIME (0)      NOT NULL,
    [Etiqueta] NVARCHAR (10) NULL,
    [Activo]   BIT           NOT NULL,
    CONSTRAINT [PK_HorasReserva] PRIMARY KEY CLUSTERED ([IdHora] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_HorasReserva_Hora]
    ON [dbo].[HorasReserva]([Hora] ASC);

