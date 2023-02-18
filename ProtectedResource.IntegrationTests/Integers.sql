USE BasicDataLayersTests;

-- Check if a table exists
IF (EXISTS (SELECT * 
				 FROM INFORMATION_SCHEMA.TABLES 
				 WHERE TABLE_SCHEMA = 'dbo' 
				 AND TABLE_NAME = 'Integers'
				 AND TABLE_TYPE = 'BASE TABLE'))
BEGIN
	DROP TABLE [dbo].[Integers]
END

CREATE TABLE [dbo].[Integers]
(
	[IntegersId] INT IDENTITY(1,1) NOT NULL,
	[Number1] INT NULL,
	[Number2] INT NULL,
	[Number3] INT NULL,
	CONSTRAINT [PK_dbo.Integers_IntegersId] PRIMARY KEY ([IntegersId])
)

-- Base value
INSERT INTO dbo.Integers
(
	Number1
   ,Number2
   ,Number3
)
VALUES
(   0 -- Number1 - int
   ,0 -- Number2 - int
   ,0 -- Number3 - int
)

SELECT * FROM dbo.Integers

RETURN

-- This is for reset
UPDATE dbo.Integers SET 
	Number1 = 0,
	Number2 = 0,
	Number3 = 0
WHERE IntegersId = 1
