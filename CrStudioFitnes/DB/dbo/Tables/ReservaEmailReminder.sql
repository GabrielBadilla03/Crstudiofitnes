CREATE TABLE [dbo].[ReservaEmailReminder] (
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [ReservaId]     INT            NOT NULL,
    [Kind]          TINYINT        NOT NULL,
    [Status]        TINYINT        NOT NULL,
    [Attempts]      INT            DEFAULT ((0)) NOT NULL,
    [NextAttemptAt] DATETIME2 (7)  NULL,
    [LastError]     NVARCHAR (MAX) NULL,
    [CreatedAtUtc]  DATETIME2 (7)  DEFAULT (sysutcdatetime()) NOT NULL,
    [SentAtUtc]     DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ReservaEmailReminder_StatusNext]
    ON [dbo].[ReservaEmailReminder]([Status] ASC, [NextAttemptAt] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_ReservaEmailReminder_Reserva_Kind]
    ON [dbo].[ReservaEmailReminder]([ReservaId] ASC, [Kind] ASC);

