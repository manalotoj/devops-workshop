CREATE TABLE [dbo].[TodoItems] (
    [Id]         BIGINT         IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (MAX) NULL,
    [IsComplete] BIT            CONSTRAINT [DF_TodoItem_IsComplete] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_TodoItem] PRIMARY KEY CLUSTERED ([Id] ASC)
);

