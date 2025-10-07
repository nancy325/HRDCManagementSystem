using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models.ViewModels;

public class AttendanceMarkItem
{
    public int TrainingRegSysID { get; set; }
    public int EmployeeSysID { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
}

public class AttendanceMarkViewModel
{
    [Required]
    public int TrainingSysID { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly AttendanceDate { get; set; }

    public string TrainingTitle { get; set; } = string.Empty;

    public List<AttendanceMarkItem> Items { get; set; } = new();

    // Indicates whether attendance has already been taken for this training/date
    public bool IsAlreadyTaken { get; set; }

    // Names of absent participants when attendance is already taken
    public List<string> AbsentParticipants { get; set; } = new();
}


