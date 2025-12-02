namespace BrightEnroll_DES.Components.Pages.Admin.Curriculum.CurriculumCS;

public class Section
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string Classroom { get; set; } = "";
    public int Capacity { get; set; }
    public string HomeroomTeacher { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class Subject
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public List<string> LinkedSections { get; set; } = new();
    public string Description { get; set; } = "";
    public List<SubjectScheduleItem> Schedules { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class SubjectScheduleItem
{
    public string SubjectCode { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public List<string> Days { get; set; } = new();
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string Description { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}

public class TeacherAssignmentModel
{
    public string Id { get; set; } = "";
    public int AssignmentId { get; set; }
    
    // Section-level summary fields (new)
    public string Grade { get; set; } = "";
    public string Section { get; set; } = "";
    public string Classroom { get; set; } = "";
    public string BuildingName { get; set; } = "";
    // Adviser assigned to this section (homeroom teacher)
    public string AdviserName { get; set; } = "";
    public int SubjectsCount { get; set; }
    public List<string> SubjectsList { get; set; } = new(); // Names of subjects in this section

    // Legacy per-assignment fields kept for backwards compatibility with existing modals
    // These are still referenced in View/Edit assignment components.
    public string TeacherName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Schedule { get; set; } = "";
    public string Time { get; set; } = "";
    public List<string> SelectedDays { get; set; } = new();
    public string Role { get; set; } = ""; // "adviser" or "subject_teacher"
}

public class TeacherAssignmentItem
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public int TeacherId { get; set; }
    public List<SubjectScheduleDisplay> Schedules { get; set; } = new();
}

public class SubjectScheduleDisplay
{
    public string DayOfWeek { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public TimeSpan StartTimeSpan { get; set; }
    public TimeSpan EndTimeSpan { get; set; }
}

public class Classroom
{
    public int ClassroomID { get; set; }
    public string RoomName { get; set; } = "";
    public string RoomType { get; set; } = "";
    public int Capacity { get; set; }
    public string BuildingName { get; set; } = "";
    public int FloorNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = "";
}

