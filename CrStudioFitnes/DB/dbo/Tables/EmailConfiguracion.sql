CREATE TABLE [dbo].[EmailConfiguracion] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [Tipo]           NVARCHAR (50)  NOT NULL,
    [Activo]         BIT            DEFAULT ((1)) NOT NULL,
    [Host]           NVARCHAR (200) NOT NULL,
    [Port]           INT            NOT NULL,
    [UseSsl]         BIT            DEFAULT ((0)) NOT NULL,
    [UseStartTls]    BIT            DEFAULT ((1)) NOT NULL,
    [FromEmail]      NVARCHAR (256) NOT NULL,
    [FromName]       NVARCHAR (120) NULL,
    [Username]       NVARCHAR (256) NOT NULL,
    [Password]       NVARCHAR (256) NOT NULL,
    [TimeoutSeconds] INT            DEFAULT ((30)) NOT NULL,
    [CreatedAtUtc]   DATETIME2 (7)  DEFAULT (sysutcdatetime()) NOT NULL,
    [UpdatedAtUtc]   DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_EmailConfiguracion_Tipo_Activo]
    ON [dbo].[EmailConfiguracion]([Tipo] ASC) WHERE ([Activo]=(1));

