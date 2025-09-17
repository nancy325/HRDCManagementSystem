using HRDCManagementSystem.Models.Entities;
using Mapster;
using System;
using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models.Request;
[AdaptTo(typeof(TrainingProgram))]
public class TrainingRequest
{
	public string Title { get; set; } = null!;

	public string TrainerName { get; set; } = null!;
    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly StartDate { get; set; }
    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly EndDate { get; set; }

	public TimeOnly FromTime { get; set; }

	public TimeOnly ToTime { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly ValidTill { get; set; }

	public string? Venue { get; set; }

	public string? EligibilityType { get; set; }

	public int Capacity { get; set; }

	public string? FilePath { get; set; }

	public string Mode { get; set; } = null!;

	public string Status { get; set; } = null!;

	public int? MarksOutOf { get; set; }

	public bool IsMarksEntry { get; set; }
}


