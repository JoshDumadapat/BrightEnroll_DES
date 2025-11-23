namespace BrightEnroll_DES.Components.Pages.Admin.Curriculum.CurriculumComponents;

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
    public string Name { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public List<string> LinkedSections { get; set; } = new();
    public string Description { get; set; } = "";
}

public class TeacherAssignmentModel
{
    public string Id { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string Grade { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Section { get; set; } = "";
    public string Schedule { get; set; } = "";
    public string Classroom { get; set; } = "";
    public string Time { get; set; } = "";
    public List<string> SelectedDays { get; set; } = new();
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

