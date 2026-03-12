-- ============================================================
-- SalonPro: Fix MonsterASP Database (db43760)
-- Run this script in MonsterASP SQL Manager
-- ============================================================

-- STEP 1: Add missing columns to Tenants table
-- (These columns are from the AddTenantSubscriptionAndVerification migration)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'EmailVerified')
BEGIN
    ALTER TABLE [Tenants] ADD [EmailVerified] bit NOT NULL DEFAULT 0;
    PRINT 'Added EmailVerified column';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'EmailVerificationToken')
BEGIN
    ALTER TABLE [Tenants] ADD [EmailVerificationToken] nvarchar(256) NULL;
    PRINT 'Added EmailVerificationToken column';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'EmailVerificationTokenExpiry')
BEGIN
    ALTER TABLE [Tenants] ADD [EmailVerificationTokenExpiry] datetime2 NULL;
    PRINT 'Added EmailVerificationTokenExpiry column';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'SubscriptionStartDate')
BEGIN
    ALTER TABLE [Tenants] ADD [SubscriptionStartDate] datetime2 NULL;
    PRINT 'Added SubscriptionStartDate column';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'SubscriptionEndDate')
BEGIN
    ALTER TABLE [Tenants] ADD [SubscriptionEndDate] datetime2 NULL;
    PRINT 'Added SubscriptionEndDate column';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'IsTrialing')
BEGIN
    ALTER TABLE [Tenants] ADD [IsTrialing] bit NOT NULL DEFAULT 0;
    PRINT 'Added IsTrialing column';
END

-- STEP 2: Add missing column to Appointments table
-- (From AddAppointmentReminderSentAt migration)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Appointments' AND COLUMN_NAME = 'ReminderSentAt')
BEGIN
    ALTER TABLE [Appointments] ADD [ReminderSentAt] datetime2 NULL;
    PRINT 'Added ReminderSentAt column';
END

-- STEP 3: Add missing Payments table if it doesn't exist
-- (From AddPaymentTable migration)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Payments')
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [PeriodStart] datetime2 NOT NULL,
        [PeriodEnd] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [PaidAt] datetime2 NULL,
        [Notes] nvarchar(2000) NULL,
        [PaidBy] nvarchar(256) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [LastModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Payments_TenantId_PeriodStart] ON [Payments] ([TenantId], [PeriodStart]);
    PRINT 'Created Payments table';
END

-- STEP 4: Update existing Demo Salon tenant to be verified
-- ============================================================

UPDATE [Tenants] 
SET [EmailVerified] = 1, 
    [IsTrialing] = 0,
    [SubscriptionStartDate] = GETUTCDATE(),
    [SubscriptionEndDate] = DATEADD(YEAR, 10, GETUTCDATE())
WHERE [EmailVerified] = 0;

-- STEP 5: Register ALL migrations as applied in __EFMigrationsHistory
-- This tells EF that these migrations are already done
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260305081442_ClientNoteTenantNoAction')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260305081442_ClientNoteTenantNoAction', '8.0.0');
    PRINT 'Registered migration: ClientNoteTenantNoAction';
END

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260305081645_StaffMemberUserNoAction')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260305081645_StaffMemberUserNoAction', '8.0.0');
    PRINT 'Registered migration: StaffMemberUserNoAction';
END

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260306123254_AddPaymentTable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260306123254_AddPaymentTable', '8.0.0');
    PRINT 'Registered migration: AddPaymentTable';
END

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260308180000_AddAppointmentReminderSentAt')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260308180000_AddAppointmentReminderSentAt', '8.0.0');
    PRINT 'Registered migration: AddAppointmentReminderSentAt';
END

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260308181000_AddTenantSubscriptionAndVerification')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260308181000_AddTenantSubscriptionAndVerification', '8.0.0');
    PRINT 'Registered migration: AddTenantSubscriptionAndVerification';
END

-- STEP 6: Verify everything is OK
-- ============================================================

PRINT '--- Applied migrations: ---';
SELECT [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId];

PRINT '--- Tenants columns: ---';
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' ORDER BY ORDINAL_POSITION;

PRINT 'DONE! Restart the app on MonsterASP now.';
