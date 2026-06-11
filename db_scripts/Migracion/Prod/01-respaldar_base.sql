-- Respaldar base de datos SIMETICS a un archivo .bak, se debe tener un volumen de host montado en /var/opt/mssql/backup para que el contenedor de SQL Server pueda escribir el archivo de respaldo.
BACKUP DATABASE [SIMETICS]
TO DISK = N'/var/opt/mssql/backup/SIMETICS_test.bak'
WITH FORMAT, INIT;

-- Restaurar la base de datos SIMETICS desde el archivo .bak
RESTORE DATABASE [SIMETICS]
FROM DISK = N'/var/opt/mssql/backup/SIMETICS_test.bak'
WITH REPLACE;
