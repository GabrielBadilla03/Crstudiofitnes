CREATE TABLE [dbo].[Historiales] (
    [IdHistorial] INT            IDENTITY (1, 1) NOT NULL,
    [IdUsuario]   NVARCHAR (450) NOT NULL,
    [FechaInicio] DATE           NOT NULL,
    [FechaFin]    DATE           NULL,
    [Estatura]    DECIMAL (6, 2) NULL,
    [Peso]        DECIMAL (6, 2) NULL,
    [Edad]        INT            NULL,
    [Estado]      NVARCHAR (50)  NULL,
    [Actividad]   NVARCHAR (50)  NULL,
    [Frecuencia]  INT            NULL,
    [Objetivo]    NVARCHAR (120) NULL,
    CONSTRAINT [PK_Historiales] PRIMARY KEY CLUSTERED ([IdHistorial] ASC),
    CONSTRAINT [FK_Historiales_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Historiales_IdUsuario]
    ON [dbo].[Historiales]([IdUsuario] ASC);

