using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using HRDCManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly HRDCContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(HRDCContext context, ICurrentUserService currentUserService, ILogger<FeedbackController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Handle different roles with different views
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(TrainingFeedbacks));
            }
            else if (User.IsInRole("Employee"))
            {
                var currentUserId = _currentUserService.GetCurrentUserId();

                // Get employee ID
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserSysID == currentUserId);

                if (employee == null)
                {
                    return NotFound("Employee record not found");
                }
                
                // Get completed trainings that are confirmed by admin
                var completedTrainings = await _context.TrainingRegistrations
                    .Include(r => r.TrainingSys)
                    .Where(r => r.EmployeeSysID == employee.EmployeeSysID && 
                                r.TrainingSys.EndDate < DateOnly.FromDateTime(DateTime.Today))
                    .ToListAsync();
                    
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.CreateUserId == currentUserId)
                    .GroupBy(f => f.TrainingRegSysID)
                    .Select(g => g.Key)
                    .ToListAsync();

                // Create list of registration IDs that have feedback
                var feedbackRegIds = feedbacks;

                var viewModel = new FeedbackHistoryViewModel
                {
                    CompletedFeedbacks = completedTrainings
                        .Where(t => feedbackRegIds.Contains(t.TrainingRegSysID))
                        .Select(t => new FeedbackTrainingItem
                        {
                            TrainingRegSysID = t.TrainingRegSysID,
                            TrainingSysID = t.TrainingSysID,
                            TrainingTitle = t.TrainingSys.Title ?? "Training",
                            EndDate = t.TrainingSys.EndDate,
                            HasFeedback = true
                        }).ToList(),

                    PendingFeedbacks = completedTrainings
                        .Where(t => !feedbackRegIds.Contains(t.TrainingRegSysID))
                        .Select(t => new FeedbackTrainingItem
                        {
                            TrainingRegSysID = t.TrainingRegSysID,
                            TrainingSysID = t.TrainingSysID,
                            TrainingTitle = t.TrainingSys.Title ?? "Training",
                            EndDate = t.TrainingSys.EndDate,
                            HasFeedback = false
                        }).ToList()
                };

                return View(viewModel);
            }

            // If user is neither Admin nor Employee, return access denied
            return Forbid();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TrainingFeedbacks()
        {
            try
            {
                // Get all trainings that have feedback responses
                var trainings = await _context.TrainingPrograms
                    .Where(t => _context.Feedbacks
                        .Include(f => f.RegSys)
                        .Any(f => f.RegSys.TrainingSysID == t.TrainingSysID))
                    .ToListAsync();
                
                var trainingsWithFeedback = new List<TrainingFeedbackSummaryViewModel>();
                
                foreach(var training in trainings)
                {
                    // Get all registration IDs for this training
                    var registrationIds = await _context.TrainingRegistrations
                        .Where(r => r.TrainingSysID == training.TrainingSysID)
                        .Select(r => r.TrainingRegSysID)
                        .ToListAsync();
                        
                    // Get all feedback for this training
                    var feedbacks = await _context.Feedbacks
                        .Include(f => f.RegSys)
                        .Include(f => f.Question)
                        .Where(f => registrationIds.Contains(f.TrainingRegSysID))
                        .ToListAsync();
                        
                    // First de-duplicate any duplicate feedback entries by grouping
                    var dedupedFeedbacks = feedbacks
                        .GroupBy(f => new { f.QuestionID, f.CreateUserId })
                        .Select(g => g.First())
                        .ToList();
                    
                    // Count unique responses (count by unique user)
                    var responseCount = dedupedFeedbacks
                        .Select(f => f.CreateUserId)
                        .Distinct()
                        .Count();
                        
                    // Calculate average rating - only consider rating questions
                    decimal averageRating = 0;
                    var ratingFeedbacks = dedupedFeedbacks
                        .Where(f => f.Question?.QuestionType == "Rating" && f.RatingValue.HasValue);
                        
                    if(ratingFeedbacks.Any())
                    {
                        averageRating = ratingFeedbacks.Average(f => f.RatingValue ?? 0);
                    }
                    
                    trainingsWithFeedback.Add(new TrainingFeedbackSummaryViewModel
                    {
                        TrainingSysID = training.TrainingSysID,
                        TrainingTitle = training.Title ?? "Unknown Training",
                        EndDate = training.EndDate,
                        ResponseCount = responseCount,
                        AverageRating = averageRating
                    });
                }

                return View(trainingsWithFeedback.OrderByDescending(t => t.EndDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading training feedbacks");
                TempData["ErrorMessage"] = "Error loading training feedbacks. Please try again.";
                return View(new List<TrainingFeedbackSummaryViewModel>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddQuestion()
        {
            // Check if current user is properly authenticated
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "User authentication issue. Please log out and log back in.";
                return RedirectToAction(nameof(ViewQuestions));
            }

            var viewModel = new FeedbackQuestionViewModel();
            await LoadTrainingList(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(FeedbackQuestionViewModel model)
        {
            // Check if current user is properly authenticated
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "User authentication issue. Please log out and log back in.";
                return RedirectToAction(nameof(ViewQuestions));
            }

            // Log the model data
            _logger.LogInformation("Adding question: {@QuestionData}", new
            {
                model.QuestionText,
                model.QuestionType,
                model.IsCommon,
                model.TrainingSysID,
                model.IsActive,
                CurrentUserId = currentUserId
            });

            // Validate required fields manually if necessary
            if (string.IsNullOrWhiteSpace(model.QuestionText))
            {
                ModelState.AddModelError("QuestionText", "Question text is required");
            }

            if (string.IsNullOrWhiteSpace(model.QuestionType))
            {
                ModelState.AddModelError("QuestionType", "Question type is required");
            }

            // If not a common question, validate training selection
            if (!model.IsCommon && !model.TrainingSysID.HasValue)
            {
                ModelState.AddModelError("TrainingSysID", "Please select a training or mark as common question");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when adding question: {@ModelErrors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                await LoadTrainingList(model);
                return View(model);
            }

            // If it's a common question, set TrainingSysID to null
            if (model.IsCommon)
            {
                model.TrainingSysID = null;
            }

            try
            {
                // IMPORTANT: Create the entity WITHOUT specifying QuestionID
                var question = new FeedbackQuestion
                {
                    // DO NOT set QuestionID here - let the database generate it
                    QuestionText = model.QuestionText,
                    IsActive = model.IsActive,
                    TrainingSysID = model.TrainingSysID,
                    QuestionType = model.QuestionType,
                    IsCommon = model.IsCommon
                    // No need to set audit fields - they'll be set by the context's SaveChanges method
                };

                _logger.LogInformation("Adding new FeedbackQuestion entity");

                // Add the entity directly without setting any tracking state
                _context.FeedbackQuestions.Add(question);

                _logger.LogInformation("Saving question to database");
                await _context.SaveChangesAsync();

                _logger.LogInformation("Question saved successfully with ID: {QuestionId}", question.QuestionID);

                TempData["SuccessMessage"] = "Feedback question added successfully.";
                return RedirectToAction(nameof(ViewQuestions));
            }
            catch (Exception ex)
            {
                // Log the detailed exception
                _logger.LogError(ex, "Error adding question: {ErrorMessage}", ex.Message);

                // Get the innermost exception for more details
                var innerException = ex;
                while (innerException.InnerException != null)
                {
                    innerException = innerException.InnerException;
                    _logger.LogError("Inner exception: {ErrorMessage}", innerException.Message);
                }

                ModelState.AddModelError("", $"Error adding question: {innerException.Message}");
                await LoadTrainingList(model);
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewQuestions()
        {
            try
            {
                var questions = await _context.FeedbackQuestions
                    .Select(q => new FeedbackQuestionViewModel
                    {
                        QuestionID = q.QuestionID,
                        QuestionText = q.QuestionText ?? string.Empty,
                        IsActive = q.IsActive,
                        IsCommon = q.IsCommon,
                        TrainingSysID = q.TrainingSysID,
                        QuestionType = q.QuestionType ?? "Rating",
                        TrainingTitle = q.TrainingSysID.HasValue ?
                            _context.TrainingPrograms
                                .Where(t => t.TrainingSysID == q.TrainingSysID)
                                .Select(t => t.Title)
                                .FirstOrDefault() ?? "Unknown Training" : "Common Question"
                    })
                    .ToListAsync();

                return View(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feedback questions");
                TempData["ErrorMessage"] = "Error loading feedback questions. Please try again.";
                return View(new List<FeedbackQuestionViewModel>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditQuestion(int id)
        {
            try
            {
                var question = await _context.FeedbackQuestions.FindAsync(id);
                if (question == null)
                {
                    return NotFound();
                }

                var viewModel = new FeedbackQuestionViewModel
                {
                    QuestionID = question.QuestionID,
                    QuestionText = question.QuestionText ?? string.Empty,
                    IsActive = question.IsActive,
                    IsCommon = question.IsCommon,
                    TrainingSysID = question.TrainingSysID,
                    QuestionType = question.QuestionType ?? "Rating"
                };

                await LoadTrainingList(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading question for editing: {QuestionId}", id);
                TempData["ErrorMessage"] = "Error loading question. Please try again.";
                return RedirectToAction(nameof(ViewQuestions));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(FeedbackQuestionViewModel model)
        {
            // Check if current user is properly authenticated
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "User authentication issue. Please log out and log back in.";
                return RedirectToAction(nameof(ViewQuestions));
            }

            // Validate required fields manually if necessary
            if (string.IsNullOrWhiteSpace(model.QuestionText))
            {
                ModelState.AddModelError("QuestionText", "Question text is required");
            }

            if (string.IsNullOrWhiteSpace(model.QuestionType))
            {
                ModelState.AddModelError("QuestionType", "Question type is required");
            }

            // If not a common question, validate training selection
            if (!model.IsCommon && !model.TrainingSysID.HasValue)
            {
                ModelState.AddModelError("TrainingSysID", "Please select a training or mark as common question");
            }

            if (!ModelState.IsValid)
            {
                await LoadTrainingList(model);
                return View(model);
            }

            try
            {
                var question = await _context.FeedbackQuestions.FindAsync(model.QuestionID);
                if (question == null)
                {
                    return NotFound();
                }

                // If it's a common question, set TrainingSysID to null
                if (model.IsCommon)
                {
                    model.TrainingSysID = null;
                }

                question.QuestionText = model.QuestionText;
                question.IsActive = model.IsActive;
                question.IsCommon = model.IsCommon;
                question.TrainingSysID = model.TrainingSysID;
                question.QuestionType = model.QuestionType;

                // Update audit fields explicitly
                question.ModifiedDateTime = DateTime.Now;
                question.ModifiedUserId = currentUserId;

                _context.Database.BeginTransaction();
                _context.Update(question);
                await _context.SaveChangesAsync();
                _context.Database.CommitTransaction();

                TempData["SuccessMessage"] = "Feedback question updated successfully.";
                return RedirectToAction(nameof(ViewQuestions));
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();

                // Log the detailed exception
                _logger.LogError(ex, "Error updating question: {ErrorMessage}", ex.Message);

                // Get the innermost exception for more details
                var innerException = ex;
                while (innerException.InnerException != null)
                {
                    innerException = innerException.InnerException;
                    _logger.LogError("Inner exception: {ErrorMessage}", innerException.Message);
                }

                ModelState.AddModelError("", $"Error updating question: {innerException.Message}");
                await LoadTrainingList(model);
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleQuestionStatus(int id)
        {
            try
            {
                var question = await _context.FeedbackQuestions.FindAsync(id);
                if (question == null)
                {
                    return NotFound();
                }

                question.IsActive = !question.IsActive;

                // Update audit fields
                question.ModifiedDateTime = DateTime.Now;
                question.ModifiedUserId = _currentUserService.GetCurrentUserId();

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ViewQuestions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling question status: {QuestionId}", id);
                TempData["ErrorMessage"] = "Error toggling question status. Please try again.";
                return RedirectToAction(nameof(ViewQuestions));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            try
            {
                var question = await _context.FeedbackQuestions
                    .Include(q => q.Feedbacks)
                    .FirstOrDefaultAsync(q => q.QuestionID == id);

                if (question == null)
                {
                    return NotFound();
                }

                // If the question has feedback responses, don't allow deletion
                if (question.Feedbacks.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete question as it has responses. Consider deactivating it instead.";
                    return RedirectToAction(nameof(ViewQuestions));
                }

                _context.Database.BeginTransaction();
                _context.FeedbackQuestions.Remove(question);
                await _context.SaveChangesAsync();
                _context.Database.CommitTransaction();

                TempData["SuccessMessage"] = "Feedback question deleted successfully.";
                return RedirectToAction(nameof(ViewQuestions));
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                _logger.LogError(ex, "Error deleting question: {QuestionId}", id);
                TempData["ErrorMessage"] = "Error deleting question. Please try again.";
                return RedirectToAction(nameof(ViewQuestions));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewFeedback(int trainingSysId)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == trainingSysId);

            if (training == null)
            {
                return NotFound();
            }

            // Get all registrations for this training
            var registrationIds = await _context.TrainingRegistrations
                .Where(r => r.TrainingSysID == trainingSysId)
                .Select(r => r.TrainingRegSysID)
                .ToListAsync();

            // Get feedback from these registrations
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Question)
                .Include(f => f.RegSys)
                .ThenInclude(r => r.EmployeeSys)
                .Where(f => registrationIds.Contains(f.TrainingRegSysID))
                .ToListAsync();

            // Group feedbacks by question and user to remove duplicates
            var dedupedFeedbacks = feedbacks
                .Where(f => f.Question != null) // Ensure question is not null
                .GroupBy(f => new { f.QuestionID, f.CreateUserId })
                .Select(g => g.First())
                .ToList();

            // Group feedbacks by question
            var questionSummaries = new List<FeedbackQuestionSummaryViewModel>();

            var questionGroups = feedbacks
                .Where(f => f.Question != null) // Ensure question is not null
                .GroupBy(f => new { 
                    f.QuestionID, 
                    QuestionText = f.Question?.QuestionText ?? "Unknown Question", 
                    QuestionType = f.Question?.QuestionType ?? "Unknown"
                });

            foreach (var group in questionGroups)
            {
                var summary = new FeedbackQuestionSummaryViewModel
                {
                    QuestionID = group.Key.QuestionID,
                    QuestionText = group.Key.QuestionText,
                    QuestionType = group.Key.QuestionType,
                    ResponseCount = group.Count()
                };

                if (group.Key.QuestionType == "Rating")
                {
                    summary.AverageRating = group.Average(f => f.RatingValue ?? 0);
                }
                else
                {
                    summary.TextResponses = group
                        .Where(f => !string.IsNullOrWhiteSpace(f.ResponseText))
                        .Select(f => f.ResponseText ?? string.Empty)
                        .ToList();
                }

                questionSummaries.Add(summary);
            }

            var viewModel = new ViewFeedbackViewModel
            {
                TrainingSysID = trainingSysId,
                TrainingTitle = training.Title ?? "Training",
                QuestionSummaries = questionSummaries
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> SubmitFeedback(int trainingRegSysId)
        {
            // Verify the training registration exists and belongs to this employee
            var currentUserId = _currentUserService.GetCurrentUserId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserSysID == currentUserId);

            if (employee == null)
            {
                return NotFound("Employee record not found");
            }

            var registration = await _context.TrainingRegistrations
                .Include(r => r.TrainingSys)
                .FirstOrDefaultAsync(r => r.TrainingRegSysID == trainingRegSysId &&
                                           r.EmployeeSysID == employee.EmployeeSysID);

            if (registration == null)
            {
                return NotFound("Training registration not found");
            }
            
            // Check if feedback already submitted
            var existingFeedback = await _context.Feedbacks
                .AnyAsync(f => f.TrainingRegSysID == trainingRegSysId &&
                               f.CreateUserId == currentUserId);

            if (existingFeedback)
            {
                TempData["ErrorMessage"] = "You have already submitted feedback for this training.";
                return RedirectToAction("Index");
            }

            // Get common questions
            var commonQuestions = await _context.FeedbackQuestions
                .Where(q => q.IsCommon && q.IsActive)
                .ToListAsync();

            // Get training-specific questions
            var trainingQuestions = await _context.FeedbackQuestions
                .Where(q => q.TrainingSysID == registration.TrainingSysID && q.IsActive)
                .ToListAsync();

            // Combine both lists
            var allQuestions = commonQuestions.Concat(trainingQuestions).ToList();

            // If no questions are defined, create default questions
            if (!allQuestions.Any())
            {
                // Create default questions if none exist
                var defaultQuestions = new List<FeedbackResponseViewModel>
                {
                    new FeedbackResponseViewModel
                    {
                        QuestionID = 0,
                        QuestionText = "How would you rate the training content?",
                        QuestionType = "Rating"
                    },
                    new FeedbackResponseViewModel
                    {
                        QuestionID = 0,
                        QuestionText = "How would you rate the trainer?",
                        QuestionType = "Rating"
                    },
                    new FeedbackResponseViewModel
                    {
                        QuestionID = 0,
                        QuestionText = "Do you have any additional comments?",
                        QuestionType = "Text"
                    }
                };

                var viewModel = new FeedbackSubmissionViewModel
                {
                    TrainingSysID = registration.TrainingSysID,
                    TrainingRegSysID = trainingRegSysId,
                    TrainingTitle = registration.TrainingSys?.Title ?? "Training",
                    Responses = defaultQuestions
                };

                return View(viewModel);
            }

            // Create view model with all questions
            var responses = allQuestions.Select(q => new FeedbackResponseViewModel
            {
                QuestionID = q.QuestionID,
                QuestionText = q.QuestionText ?? string.Empty,
                QuestionType = q.QuestionType ?? "Rating"
            }).ToList();

            var model = new FeedbackSubmissionViewModel
            {
                TrainingSysID = registration.TrainingSysID,
                TrainingRegSysID = trainingRegSysId,
                TrainingTitle = registration.TrainingSys?.Title ?? "Training",
                Responses = responses
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFeedback(FeedbackSubmissionViewModel model)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();

            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "User authentication issue. Please log out and log back in.";
                return RedirectToAction("Index");
            }
            
            // Verify the user is still confirmed for this registration
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserSysID == currentUserId);
                
            if (employee == null)
            {
                return NotFound("Employee record not found");
            }
            
            var registration = await _context.TrainingRegistrations
                .FirstOrDefaultAsync(r => r.TrainingRegSysID == model.TrainingRegSysID && 
                                      r.EmployeeSysID == employee.EmployeeSysID);
                                      
            if (registration == null)
            {
                TempData["ErrorMessage"] = "Training registration not found.";
                return RedirectToAction("Index");
            }
            
            // Check if the employee is confirmed by admin for this training
            if (!registration.Confirmation)
            {
                TempData["ErrorMessage"] = "Your registration has not been confirmed by the administrator yet. Feedback submission is only available for confirmed participants.";
                return RedirectToAction("Index");
            }

            // Check if any validation errors exist
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when submitting feedback: {@ModelErrors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                return View("SubmitFeedback", model);
            }

            // Manually validate rating questions have values
            bool hasValidationErrors = false;
            for (int i = 0; i < model.Responses.Count; i++)
            {
                var response = model.Responses[i];
                if (response.QuestionType == "Rating" && (!response.RatingValue.HasValue || response.RatingValue < 1))
                {
                    ModelState.AddModelError($"Responses[{i}].RatingValue", "Please provide a rating");
                    hasValidationErrors = true;
                }
            }

            if (hasValidationErrors)
            {
                return View("SubmitFeedback", model);
            }

            // Check if feedback already exists for this registration
            var existingFeedback = await _context.Feedbacks
                .AnyAsync(f => f.TrainingRegSysID == model.TrainingRegSysID && 
                               f.CreateUserId == currentUserId);
                               
            if (existingFeedback)
            {
                TempData["ErrorMessage"] = "You have already submitted feedback for this training.";
                return RedirectToAction("Index");
            }

            // Use a separate transaction to avoid potential state tracking issues
            using (var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    _logger.LogInformation("Processing feedback submission with {ResponseCount} responses", model.Responses.Count);
                    
                    // Process each response
                    foreach (var response in model.Responses)
                    {
                        _logger.LogInformation("Processing response: QuestionID={QuestionID}, QuestionType={QuestionType}",
                            response.QuestionID, response.QuestionType);

                        // For default questions that don't exist in the database
                        if (response.QuestionID == 0)
                        {
                            // Create the question first with minimal properties
                            var newQuestion = new FeedbackQuestion
                            {
                                QuestionText = response.QuestionText,
                                IsActive = true,
                                QuestionType = response.QuestionType,
                                IsCommon = true,
                                CreateDateTime = DateTime.Now,
                                CreateUserId = currentUserId
                            };

                            _context.FeedbackQuestions.Add(newQuestion);
                            await _context.SaveChangesAsync();

                            response.QuestionID = newQuestion.QuestionID;
                            _logger.LogInformation("Created new question with ID: {QuestionID}", newQuestion.QuestionID);
                        }

                        // Create feedback with minimal properties
                        var feedback = new Feedback
                        {
                            TrainingRegSysID = model.TrainingRegSysID,
                            QuestionID = response.QuestionID,
                            RatingValue = response.QuestionType == "Rating" ? response.RatingValue : null,
                            ResponseText = response.QuestionType == "Text" ? response.ResponseText : null,
                            CreateDateTime = DateTime.Now,
                            CreateUserId = currentUserId
                        };

                        _context.Feedbacks.Add(feedback);
                        _logger.LogInformation("Added feedback for QuestionID={QuestionID}", response.QuestionID);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Feedback submitted successfully for TrainingRegSysID={TrainingRegSysID}",
                        model.TrainingRegSysID);

                    TempData["SuccessMessage"] = "Thank you for your feedback!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error saving feedback: {ErrorMessage}, TrainingRegSysID={TrainingRegSysID}",
                        ex.Message, model.TrainingRegSysID);

                    var innerException = ex;
                    while (innerException.InnerException != null)
                    {
                        innerException = innerException.InnerException;
                        _logger.LogError("Inner exception: {ErrorMessage}", innerException.Message);
                    }

                    TempData["ErrorMessage"] = $"Error saving feedback: {innerException.Message}. Please try again.";
                    return View("SubmitFeedback", model);
                }
            }
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ViewMyFeedback(int trainingRegSysId)
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            
            var registration = await _context.TrainingRegistrations
                .Include(r => r.TrainingSys)
                .FirstOrDefaultAsync(r => r.TrainingRegSysID == trainingRegSysId);
                
            if (registration == null)
            {
                return NotFound("Training registration not found");
            }
            
            // Check if the registration is confirmed
            if (!registration.Confirmation)
            {
                TempData["ErrorMessage"] = "You can only view feedback for confirmed training registrations.";
                return RedirectToAction("Index");
            }

            // Get feedback entries, group by question to avoid duplicates
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Question)
                .Where(f => f.TrainingRegSysID == trainingRegSysId &&
                            f.CreateUserId == currentUserId)
                .ToListAsync();

            if (!feedbacks.Any())
            {
                TempData["ErrorMessage"] = "No feedback found for this training.";
                return RedirectToAction("Index");
            }
            
            var groupedResponses = feedbacks
                .Where(f => f.Question != null)
                .GroupBy(f => f.QuestionID)
                .Select(g => g.First()); // Take the first entry for each question
            
            var responses = groupedResponses
                .Select(f => new FeedbackResponseViewModel
                {
                    QuestionID = f.QuestionID,
                    QuestionText = f.Question?.QuestionText ?? "Unknown Question",
                    QuestionType = f.Question?.QuestionType ?? "Unknown",
                    RatingValue = f.RatingValue,
                    ResponseText = f.ResponseText
                }).ToList();

            var viewModel = new FeedbackSubmissionViewModel
            {
                TrainingRegSysID = trainingRegSysId,
                TrainingSysID = registration.TrainingSysID,
                TrainingTitle = registration.TrainingSys?.Title ?? "Training",
                Responses = responses
            };

            return View(viewModel);
        }

        private async Task LoadTrainingList(FeedbackQuestionViewModel viewModel)
        {
            try
            {
                viewModel.TrainingList = await _context.TrainingPrograms
                    .Where(t => t.RecStatus == "active")
                    .Select(t => new SelectListItem
                    {
                        Value = t.TrainingSysID.ToString(),
                        Text = t.Title ?? "Unknown Training"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading training list");
                viewModel.TrainingList = new List<SelectListItem>();
            }
        }
    }
}