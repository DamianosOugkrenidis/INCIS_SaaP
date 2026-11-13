/* =====================================================================
   07_scheduled_backup.sql  (ASCII-only)
   Purpose: automated FULL + LOG backup with CHECKSUM and VERIFYONLY,
            plus retention cleanup. Intended to be called by SQL Agent
            (daily full; every 15 min log) or a scheduler.
   Requirements:
     - Database INCIS_SaaP in FULL recovery model (for log backups).
     - Approved backup directory with enough free space.
     - Account with BACKUP rights and access to @BackupDir.
   Notes:
     - SQL Express does NOT support WITH COMPRESSION (Msg 1844).
     - Adjust @BackupDir / @RetentionDays before scheduling.
   ===================================================================== */
SET NOCOUNT ON;

DECLARE @BackupDir     nvarchar(400) = N'C:\Backups\INCIS';
DECLARE @RetentionDays int           = 30;
DECLARE @Stamp         nvarchar(20)  =
    CONVERT(nvarchar(8), GETDATE(), 112) + N'_' + REPLACE(CONVERT(nvarchar(8), GETDATE(), 108), N':', N'');
DECLARE @FullFile      nvarchar(500) = @BackupDir + N'\INCIS_SaaP_FULL_' + @Stamp + N'.bak';
DECLARE @LogFile       nvarchar(500) = @BackupDir + N'\INCIS_SaaP_LOG_'  + @Stamp + N'.trn';

PRINT '=== Scheduled backup: ' + @Stamp + ' ===';

IF DB_ID(N'INCIS_SaaP') IS NULL
BEGIN
    PRINT 'ERROR: database INCIS_SaaP not found. Aborting.';
    RETURN;
END

IF (SELECT recovery_model_desc FROM sys.databases WHERE name = N'INCIS_SaaP') <> N'FULL'
    PRINT 'WARNING: INCIS_SaaP is not in FULL recovery; LOG backup may fail. Set FULL for point-in-time recovery.';

BEGIN TRY
    BACKUP DATABASE [INCIS_SaaP] TO DISK = @FullFile
        WITH INIT, CHECKSUM, STATS = 25;
    PRINT 'FULL backup OK: ' + @FullFile;

    RESTORE VERIFYONLY FROM DISK = @FullFile WITH CHECKSUM;
    PRINT 'VERIFYONLY OK.';

    BACKUP LOG [INCIS_SaaP] TO DISK = @LogFile
        WITH INIT, CHECKSUM, STATS = 25;
    PRINT 'LOG backup OK: ' + @LogFile;
END TRY
BEGIN CATCH
    PRINT 'BACKUP FAILED: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

/* Retention cleanup (best-effort). Requires xp_delete_file permissions.
   Deletes files older than @RetentionDays in @BackupDir. */
BEGIN TRY
    EXEC master.dbo.xp_delete_file 0, @BackupDir, N'bak', DATEADD(DAY, -@RetentionDays, GETDATE());
    EXEC master.dbo.xp_delete_file 0, @BackupDir, N'trn', DATEADD(DAY, -@RetentionDays, GETDATE());
    PRINT 'Retention cleanup done (older than ' + CONVERT(nvarchar(10), @RetentionDays) + ' days).';
END TRY
BEGIN CATCH
    PRINT 'Retention cleanup skipped: ' + ERROR_MESSAGE();
END CATCH;

PRINT '=== Done ===';
