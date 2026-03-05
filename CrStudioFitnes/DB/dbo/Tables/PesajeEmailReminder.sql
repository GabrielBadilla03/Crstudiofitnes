CREATE TABLE [dbo].[PesajeEmailReminder] (
    [Id]            INT             IDENTITY (1, 1) NOT NULL,
    [HistorialId]   INT             NOT NULL,
    [DueDate]       DATE            NOT NULL,
    [Kind]          INT             NOT NULL,
    [Status]        INT             CONSTRAINT [DF_PesajeEmailReminder_Status] DEFAULT ((0)) NOT NULL,
    [Attempts]      INT             CONSTRAINT [DF_PesajeEmailReminder_Attempts] DEFAULT ((0)) NOT NULL,
    [CreatedAtUtc]  DATETIME2 (0)   CONSTRAINT [DF_PesajeEmailReminder_CreatedAtUtc] DEFAULT (sysutcdatetime()) NOT NULL,
    [SentAtUtc]     DATETIME2 (0)   NULL,
    [LastError]     NVARCHAR (1000) NULL,
    [NextAttemptAt] DATETIME2 (0)   NULL,
    CONSTRAINT [PK_PesajeEmailReminder] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PesajeEmailReminder_Historial] FOREIGN KEY ([HistorialId]) REFERENCES [dbo].[Historiales] ([IdHistorial]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_PesajeEmailReminder_Status_NextAttemptAt]
    ON [dbo].[PesajeEmailReminder]([Status] ASC, [NextAttemptAt] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_PesajeEmailReminder_Historial_DueDate_Kind]
    ON [dbo].[PesajeEmailReminder]([HistorialId] ASC, [DueDate] ASC, [Kind] ASC);

