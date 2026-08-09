-- Run this first, once, in SQL Server Management Studio / Azure Data Studio / sqlcmd
-- connected to your SQL Server instance (master database context).

IF DB_ID(N'SocietyGatekeeperDb') IS NULL
BEGIN
    CREATE DATABASE [SocietyGatekeeperDb];
END;
GO
