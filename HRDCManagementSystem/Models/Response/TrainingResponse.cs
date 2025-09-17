using Mapster;
using System;
using HRDCManagementSystem.Models.Entities;

namespace HRDCManagementSystem.Models.Response;

[AdaptFrom(typeof(TrainingProgram))]
public class TrainingResponse
{
	public int TrainingSysID { get; set; }

	public string Title { get; set; } = null!;

	public string TrainerName { get; set; } = null!;

	public DateOnly StartDate { get; set; }

	public DateOnly EndDate { get; set; }

	public TimeOnly FromTime { get; set; }

	public TimeOnly ToTime { get; set; }

	public DateOnly ValidTill { get; set; }

	public string? Venue { get; set; }

	public string? EligibilityType { get; set; }

	public int Capacity { get; set; }

	//public string? FilePath { get; set; }

	public string Mode { get; set; } = null!;

	public string Status { get; set; } = null!;

	public int? MarksOutOf { get; set; }

	//public bool? IsMarksEntry { get; set; }
}


