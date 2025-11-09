using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models.ViewModels
{
    public class FeedbackQuestionViewModel
    {
        // Only set this property for editing existing questions
        public int QuestionID { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [Display(Name = "Question")]
        public string QuestionText { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Common Question")]
        public bool IsCommon { get; set; }

        [Display(Name = "Training")]
        public int? TrainingSysID { get; set; }

        [Required(ErrorMessage = "Question type is required")]
        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "Rating";

        // For dropdown in the form
        public List<SelectListItem> TrainingList { get; set; } = new();

        public List<SelectListItem> QuestionTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem("Rating", "Rating"),
            new SelectListItem("Text", "Text")
        };

        public string TrainingTitle { get; set; } = "Common Question";
    }

    public class FeedbackResponseViewModel
    {
        public int QuestionID { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string QuestionType { get; set; } = "Rating";

        public decimal? RatingValue { get; set; }

        public string? ResponseText { get; set; }
    }

    public class FeedbackSubmissionViewModel
    {
        public int TrainingSysID { get; set; }

        public int TrainingRegSysID { get; set; }

        public string TrainingTitle { get; set; } = string.Empty;

        public List<FeedbackResponseViewModel> Responses { get; set; } = new();
    }

    public class ViewFeedbackViewModel
    {
        public int TrainingSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public List<FeedbackQuestionSummaryViewModel> QuestionSummaries { get; set; } = new();
    }

    public class FeedbackQuestionSummaryViewModel
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        public List<string> TextResponses { get; set; } = new();
        public int ResponseCount { get; set; }
    }

    public class FeedbackHistoryViewModel
    {
        public List<FeedbackTrainingItem> CompletedFeedbacks { get; set; } = new();
        public List<FeedbackTrainingItem> PendingFeedbacks { get; set; } = new();
    }

    public class FeedbackTrainingItem
    {
        public int TrainingRegSysID { get; set; }
        public int TrainingSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public DateOnly EndDate { get; set; }
        public bool HasFeedback { get; set; }
    }

    public class TrainingFeedbackSummaryViewModel
    {
        public int TrainingSysID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public DateOnly EndDate { get; set; }
        public int ResponseCount { get; set; }
        public decimal AverageRating { get; set; }
    }
}