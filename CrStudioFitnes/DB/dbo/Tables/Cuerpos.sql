CREATE TABLE [dbo].[Cuerpos] (
    [IdCuerpo] INT            IDENTITY (1, 1) NOT NULL,
    [Nombre]   NVARCHAR (60)  NOT NULL,
    [Detalle]  NVARCHAR (120) NULL,
    CONSTRAINT [PK_Cuerpos] PRIMARY KEY CLUSTERED ([IdCuerpo] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Cuerpos_Nombre]
    ON [dbo].[Cuerpos]([Nombre] ASC);

