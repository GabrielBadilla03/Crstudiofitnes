CREATE TABLE [dbo].[Paquetes] (
    [IdPaquete]     INT             IDENTITY (1, 1) NOT NULL,
    [CantDias]      NVARCHAR (MAX)  NOT NULL,
    [CantLecciones] INT             NOT NULL,
    [Pago]          DECIMAL (10, 2) NOT NULL,
    [Detalle]       NVARCHAR (200)  NULL,
    [Activo]        BIT             NOT NULL,
    CONSTRAINT [PK_Paquetes] PRIMARY KEY CLUSTERED ([IdPaquete] ASC)
);

