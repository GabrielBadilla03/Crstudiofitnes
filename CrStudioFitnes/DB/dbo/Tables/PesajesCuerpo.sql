CREATE TABLE [dbo].[PesajesCuerpo] (
    [IdPesaje] INT            NOT NULL,
    [IdCuerpo] INT            NOT NULL,
    [Medida]   DECIMAL (7, 2) NOT NULL,
    CONSTRAINT [PK_PesajesCuerpo] PRIMARY KEY CLUSTERED ([IdPesaje] ASC, [IdCuerpo] ASC),
    CONSTRAINT [FK_PesajesCuerpo_Cuerpos_IdCuerpo] FOREIGN KEY ([IdCuerpo]) REFERENCES [dbo].[Cuerpos] ([IdCuerpo]),
    CONSTRAINT [FK_PesajesCuerpo_Pesajes_IdPesaje] FOREIGN KEY ([IdPesaje]) REFERENCES [dbo].[Pesajes] ([IdPesaje]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_PesajesCuerpo_IdCuerpo]
    ON [dbo].[PesajesCuerpo]([IdCuerpo] ASC);

