# Gradebook Critical Changes - Quick Reference

## 🚨 IMMEDIATE ACTION REQUIRED

### 1. FIX GRADE COMPONENTS (CRITICAL)

**Current (WRONG):**
```
Quiz (30%) + Exam (40%) + Project (20%) + Participation (10%)
```

**Required (DepEd Standard):**
```
Written Work (20%) + Performance Tasks (60%) + Quarterly Assessment (20%)
```

**Files to Update:**
- `Data/Models/Grade.cs` - Replace properties
- `Data/Models/GradeWeight.cs` - Update weights
- `Services/Business/Academic/GradeService.cs` - Update computation
- `Components/Pages/Admin/Gradebook/GradebookComponents/GradeEntry.razor` - Update UI

---

### 2. FIX TRANSMUTATION (CRITICAL)

**Current (WRONG):**
```csharp
if (rawGrade >= 0.00m && rawGrade <= 45.99m) return 50.00m;
```

**Required (DepEd Standard):**
```csharp
// Minimum passing grade is 60, transmuted to 75
if (rawGrade >= 0.00m && rawGrade < 60.00m) return 75.00m;
```

**File to Update:**
- `Services/Business/Academic/GradeService.cs` - Line 634-649

---

### 3. IMPLEMENT QUARTERLY FORMULA (CRITICAL)

**Current:** Simple average of components

**Required (DepEd Formula):**
```csharp
Quarterly Grade = (WrittenWork × 0.20) + (PerformanceTasks × 0.60) + (QuarterlyAssessment × 0.20)
Final Grade = Average of Q1, Q2, Q3, Q4
```

**File to Update:**
- `Services/Business/Academic/GradeService.cs` - Update `CalculateFinalGrade` method

---

### 4. ADD GRADE APPROVAL WORKFLOW (HIGH PRIORITY)

**Required Models:**
```csharp
// Add to Grade model:
public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Locked
public DateTime? SubmittedAt { get; set; }
public DateTime? ApprovedAt { get; set; }
public int? ApprovedBy { get; set; }

// New model:
public class GradeApproval
{
    public int ApprovalId { get; set; }
    public int GradeId { get; set; }
    public int ApprovedBy { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string Comments { get; set; }
    public string Status { get; set; }
}
```

---

### 5. IMPLEMENT PDF REPORT CARDS (HIGH PRIORITY)

**Current:** TODO comment only

**Required:**
- Use QuestPDF or iTextSharp
- Create DepEd-compliant template
- Include all subjects, Q1-Q4, transmuted grades, descriptive ratings
- Add school info, signatures

**File to Update:**
- `Components/Pages/Admin/Gradebook/GradebookComponents/ReportCards.razor` - Line 215-229

---

## 📋 DATABASE MIGRATION CHECKLIST

### Step 1: Backup Database
```sql
BACKUP DATABASE [YourDatabase] TO DISK = 'backup.bak'
```

### Step 2: Add New Columns
```sql
-- Add new grade component columns
ALTER TABLE tbl_Grades
ADD written_work DECIMAL(5,2) NULL,
    performance_tasks DECIMAL(5,2) NULL,
    quarterly_assessment DECIMAL(5,2) NULL,
    status VARCHAR(20) DEFAULT 'Draft',
    submitted_at DATETIME NULL,
    approved_at DATETIME NULL,
    approved_by INT NULL;

-- Create approval table
CREATE TABLE tbl_GradeApprovals (
    approval_id INT IDENTITY(1,1) PRIMARY KEY,
    grade_id INT NOT NULL,
    approved_by INT NOT NULL,
    approved_at DATETIME NOT NULL DEFAULT GETDATE(),
    comments NVARCHAR(500) NULL,
    status VARCHAR(20) NOT NULL,
    FOREIGN KEY (grade_id) REFERENCES tbl_Grades(grade_id),
    FOREIGN KEY (approved_by) REFERENCES tbl_Users(user_ID)
);
```

### Step 3: Migrate Existing Data (if any)
```sql
-- Map old components to new (example - adjust based on your logic)
UPDATE tbl_Grades
SET written_work = COALESCE(quiz, 0),
    performance_tasks = COALESCE(project, 0),
    quarterly_assessment = COALESCE(exam, 0)
WHERE written_work IS NULL;
```

### Step 4: Remove Old Columns (after verification)
```sql
-- Only after confirming new columns work correctly
ALTER TABLE tbl_Grades
DROP COLUMN quiz, exam, project, participation;
```

---

## 🔧 CODE CHANGES QUICK REFERENCE

### GradeService.cs Changes:

1. **Update GetGradeWeightsAsync:**
```csharp
return new GradeWeightDto
{
    SubjectId = subjectId,
    WrittenWorkWeight = 0.20m,      // Changed from QuizWeight
    PerformanceTasksWeight = 0.60m, // Changed from ExamWeight
    QuarterlyAssessmentWeight = 0.20m // Changed from ProjectWeight
};
```

2. **Update SaveGradesAsync validation:**
```csharp
// Validate all three components are present
if (!input.WrittenWork.HasValue || 
    !input.PerformanceTasks.HasValue || 
    !input.QuarterlyAssessment.HasValue)
{
    throw new ArgumentException("All grade components must be entered.");
}
```

3. **Update quarterly grade computation:**
```csharp
private decimal CalculateQuarterlyGrade(decimal? writtenWork, decimal? performanceTasks, decimal? quarterlyAssessment)
{
    if (!writtenWork.HasValue || !performanceTasks.HasValue || !quarterlyAssessment.HasValue)
        return 0;
    
    return (writtenWork.Value * 0.20m) + 
           (performanceTasks.Value * 0.60m) + 
           (quarterlyAssessment.Value * 0.20m);
}
```

---

## ✅ TESTING CHECKLIST

- [ ] Grade entry with new components works
- [ ] Quarterly grade computation matches DepEd formula
- [ ] Transmutation: 59.99 → 75 (not 50)
- [ ] Final grade = average of Q1-Q4
- [ ] Grade approval workflow works
- [ ] Grades lock after approval
- [ ] Report card PDF generates correctly
- [ ] Report card includes all required fields
- [ ] Grade validation prevents incomplete entries
- [ ] Grade history tracks all changes

---

## 📞 SUPPORT & QUESTIONS

For implementation assistance, refer to:
- `DOCUMENTATION/TEACHER_MODULE_GRADEBOOK_EVALUATION.md` - Full evaluation
- DepEd DO 8, s. 2015 - Official grading policy
- ERP Standards documentation

---

**Priority**: CRITICAL  
**Timeline**: Implement Phase 1 within 2-4 weeks  
**Impact**: Required for DepEd compliance




