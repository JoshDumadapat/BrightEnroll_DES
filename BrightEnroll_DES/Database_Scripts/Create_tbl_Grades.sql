-- Script to create tbl_Grades table if it doesn't exist
-- Run this in SQL Server Management Studio (SSMS) if the table is missing

USE [DB_BrightEnroll_DES];
GO

-- Check if table exists, if not create it
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Grades' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Grades](
        [grade_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [subject_id] INT NOT NULL,
        [section_id] INT NOT NULL,
        [school_year] VARCHAR(20) NOT NULL,
        [grading_period] VARCHAR(10) NOT NULL,
        [quiz] DECIMAL(5,2) NULL,
        [exam] DECIMAL(5,2) NULL,
        [project] DECIMAL(5,2) NULL,
        [participation] DECIMAL(5,2) NULL,
        [final_grade] DECIMAL(5,2) NULL,
        [teacher_id] INT NOT NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL,
        CONSTRAINT FK_tbl_Grades_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Grades_tbl_Subjects FOREIGN KEY ([subject_id]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Grades_tbl_Sections FOREIGN KEY ([section_id]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Grades_tbl_Users FOREIGN KEY ([teacher_id]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE RESTRICT
    );
    
    PRINT 'Table [tbl_Grades] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_Grades] already exists.';
END
GO

-- Create indexes if they don't exist
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Grades' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Index on StudentId
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_StudentId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
    BEGIN
        CREATE INDEX IX_tbl_Grades_StudentId ON [dbo].[tbl_Grades]([student_id]);
        PRINT 'Index [IX_tbl_Grades_StudentId] created.';
    END
    
    -- Index on SubjectId
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_SubjectId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
    BEGIN
        CREATE INDEX IX_tbl_Grades_SubjectId ON [dbo].[tbl_Grades]([subject_id]);
        PRINT 'Index [IX_tbl_Grades_SubjectId] created.';
    END
    
    -- Index on SectionId
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_SectionId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
    BEGIN
        CREATE INDEX IX_tbl_Grades_SectionId ON [dbo].[tbl_Grades]([section_id]);
        PRINT 'Index [IX_tbl_Grades_SectionId] created.';
    END
    
    -- Index on TeacherId
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_TeacherId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
    BEGIN
        CREATE INDEX IX_tbl_Grades_TeacherId ON [dbo].[tbl_Grades]([teacher_id]);
        PRINT 'Index [IX_tbl_Grades_TeacherId] created.';
    END
    
    -- Composite index for queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_Student_Subject_Section_Period' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
    BEGIN
        CREATE INDEX IX_tbl_Grades_Student_Subject_Section_Period ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year]);
        PRINT 'Index [IX_tbl_Grades_Student_Subject_Section_Period] created.';
    END
    
    -- Unique constraint to prevent duplicate grades
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_tbl_Grades_Student_Subject_Section_Period_Year' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
    BEGIN
        CREATE UNIQUE INDEX UX_tbl_Grades_Student_Subject_Section_Period_Year ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year]);
        PRINT 'Unique index [UX_tbl_Grades_Student_Subject_Section_Period_Year] created.';
    END
END
GO

PRINT 'Script completed.';
GO

