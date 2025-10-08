﻿using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class AssessmentController : Controller
    {
        private readonly HRDCContext _context;
        private readonly ILogger<AssessmentController> _logger;

        public AssessmentController(HRDCContext context, ILogger<AssessmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Display all trainings with Google Form Tests
        [HttpGet("")]
        public async Task<IActionResult> Index(string search)
        {
            try
            {
                _logger.LogInformation("Loading assessment index with search: {Search}", search ?? "none");

                var trainingsQuery = _context.TrainingPrograms
                    .Where(t => t.GoogleFormTestLink != null && t.RecStatus == "active");

                if (!string.IsNullOrEmpty(search))
                {
                    trainingsQuery = trainingsQuery.Where(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var trainings = await trainingsQuery.ToListAsync();

                // Map entity -> viewmodel using Mapster
                var trainingsWithTests = trainings.Adapt<List<TrainingAssessmentViewModel>>();

                _logger.LogInformation("Successfully loaded {Count} trainings for assessment", trainingsWithTests.Count);
                return View("~/Views/Admin/Assessment/Index.cshtml", trainingsWithTests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assessment index with search: {Search}", search ?? "none");
                TempData["ErrorMessage"] = "Unable to load assessment data. Please try again later.";
                return View("~/Views/Admin/Assessment/Index.cshtml", new List<TrainingAssessmentViewModel>());
            }
        }

        // Show marks entry form for a training
        [HttpGet("ManageMarks/{id}")]
        public async Task<IActionResult> ManageMarks(int id)
        {
            try
            {
                _logger.LogInformation("Loading marks entry form for training ID: {TrainingId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid training ID provided: {TrainingId}", id);
                    TempData["ErrorMessage"] = "Invalid training ID provided.";
                    return RedirectToAction(nameof(Index));
                }

                var training = await _context.TrainingPrograms
                    .Include(t => t.TrainingRegistrations)
                        .ThenInclude(r => r.EmployeeSys)
                    .Include(t => t.TrainingRegistrations)
                        .ThenInclude(r => r.Attendances)
                    .FirstOrDefaultAsync(t => t.TrainingSysID == id);

                if (training == null)
                {
                    _logger.LogWarning("Training not found with ID: {TrainingId}", id);
                    TempData["ErrorMessage"] = "Training not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(training.GoogleFormTestLink))
                {
                    _logger.LogWarning("Training {TrainingId} does not have a Google Form Test Link", id);
                    TempData["ErrorMessage"] = "This training does not have an associated Google Form test.";
                    return RedirectToAction(nameof(Index));
                }

                var confirmedRegistrations = training.TrainingRegistrations
                    .Where(r => r.RecStatus == "active" && r.Registration == true && r.Confirmation == true)
                    .ToList();

                _logger.LogInformation("Found {Count} confirmed registrations for training {TrainingId}", confirmedRegistrations.Count, id);

                var vm = new AssessmentMarksEntryViewModel
                {
                    TrainingId = training.TrainingSysID,
                    TrainingTitle = training.Title,
                    MarksOutOf = training.MarksOutOf ?? 0,
                    EmployeeMarks = confirmedRegistrations
                        .Select(r => new EmployeeMarksViewModel
                        {
                            RegistrationId = r.TrainingRegSysID,
                            EmployeeName = $"{r.EmployeeSys.FirstName} {r.EmployeeSys.LastName}",
                            Department = r.EmployeeSys.Department ?? "N/A",
                            MarksObtained = r.MarksObtained
                        })
                        .OrderBy(e => e.EmployeeName)
                        .ToList()
                };

                // Load all trainings with Google Form Tests for the dropdown
                var trainingsWithTests = await _context.TrainingPrograms
                    .Where(t => t.GoogleFormTestLink != null && t.RecStatus == "active")
                    .Select(t => new { t.TrainingSysID, t.Title })
                    .ToListAsync();

                ViewBag.Trainings = trainingsWithTests;

                _logger.LogInformation("Successfully loaded marks entry form for training {TrainingId} with {ParticipantCount} participants", 
                    id, vm.EmployeeMarks.Count);

                return View("~/Views/Admin/Assessment/ManageMarks.cshtml", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading marks entry form for training ID: {TrainingId}", id);
                TempData["ErrorMessage"] = "Unable to load marks entry form. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Save marks entered by admin
        [HttpPost("ManageMarks/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageMarks(int id, AssessmentMarksEntryViewModel model)
        {
            try
            {
                _logger.LogInformation("Saving marks for training ID: {TrainingId} with {ParticipantCount} participants", 
                    model.TrainingId, model.EmployeeMarks?.Count ?? 0);

                if (model == null)
                {
                    _logger.LogWarning("Model is null in ManageMarks POST");
                    TempData["ErrorMessage"] = "Invalid data received.";
                    return RedirectToAction(nameof(Index));
                }

                if (model.TrainingId <= 0)
                {
                    _logger.LogWarning("Invalid training ID in model: {TrainingId}", model.TrainingId);
                    TempData["ErrorMessage"] = "Invalid training ID.";
                    return RedirectToAction(nameof(Index));
                }

                if (model.EmployeeMarks == null || !model.EmployeeMarks.Any())
                {
                    _logger.LogWarning("No employee marks data provided for training {TrainingId}", model.TrainingId);
                    TempData["ErrorMessage"] = "No participant data provided.";
                    return RedirectToAction(nameof(ManageMarks), new { id = model.TrainingId });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid for training {TrainingId}", model.TrainingId);
                    await ReloadViewBagData();
                    return View("~/Views/Admin/Assessment/ManageMarks.cshtml", model);
                }

                // Additional validation: Ensure marks do not exceed MarksOutOf
                bool hasValidationErrors = false;
                for (int i = 0; i < model.EmployeeMarks.Count; i++)
                {
                    var empMark = model.EmployeeMarks[i];
                    if (empMark.MarksObtained.HasValue && empMark.MarksObtained.Value > model.MarksOutOf)
                    {
                        ModelState.AddModelError($"EmployeeMarks[{i}].MarksObtained", 
                            $"Marks obtained cannot exceed {model.MarksOutOf}.");
                        hasValidationErrors = true;
                    }

                    if (empMark.MarksObtained.HasValue && empMark.MarksObtained.Value < 0)
                    {
                        ModelState.AddModelError($"EmployeeMarks[{i}].MarksObtained", 
                            "Marks obtained cannot be negative.");
                        hasValidationErrors = true;
                    }
                }

                if (hasValidationErrors)
                {
                    _logger.LogWarning("Validation errors found for training {TrainingId}", model.TrainingId);
                    await ReloadViewBagData();
                    return View("~/Views/Admin/Assessment/ManageMarks.cshtml", model);
                }

                // Verify training exists and has Google Form Test
                var training = await _context.TrainingPrograms
                    .FirstOrDefaultAsync(t => t.TrainingSysID == model.TrainingId);

                if (training == null)
                {
                    _logger.LogWarning("Training not found during marks save: {TrainingId}", model.TrainingId);
                    TempData["ErrorMessage"] = "Training not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(training.GoogleFormTestLink))
                {
                    _logger.LogWarning("Training {TrainingId} does not have Google Form Test Link during save", model.TrainingId);
                    TempData["ErrorMessage"] = "This training does not have an associated Google Form test.";
                    return RedirectToAction(nameof(Index));
                }

                // Update marks for each participant
                int updatedCount = 0;
                foreach (var empMark in model.EmployeeMarks)
                {
                    try
                    {
                        var registration = await _context.TrainingRegistrations
                            .FirstOrDefaultAsync(r => r.TrainingRegSysID == empMark.RegistrationId);

                        if (registration != null)
                        {
                            registration.MarksObtained = empMark.MarksObtained;
                            updatedCount++;
                            _logger.LogDebug("Updated marks for registration {RegistrationId}: {Marks}", 
                                empMark.RegistrationId, empMark.MarksObtained);
                        }
                        else
                        {
                            _logger.LogWarning("Registration not found for ID: {RegistrationId}", empMark.RegistrationId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating marks for registration {RegistrationId}", empMark.RegistrationId);
                        // Continue with other registrations
                    }
                }

                if (updatedCount == 0)
                {
                    _logger.LogWarning("No registrations were updated for training {TrainingId}", model.TrainingId);
                    TempData["ErrorMessage"] = "No marks were updated. Please check the participant data.";
                    return RedirectToAction(nameof(ManageMarks), new { id = model.TrainingId });
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully saved marks for {UpdatedCount} participants in training {TrainingId}", 
                    updatedCount, model.TrainingId);
                
                TempData["SuccessMessage"] = $"Marks updated successfully for {updatedCount} participant(s)!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving marks for training {TrainingId}", model?.TrainingId);
                TempData["ErrorMessage"] = "Database error occurred while saving marks. Please try again.";
                return RedirectToAction(nameof(ManageMarks), new { id = model?.TrainingId ?? 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while saving marks for training {TrainingId}", model?.TrainingId);
                TempData["ErrorMessage"] = "An unexpected error occurred while saving marks. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to reload ViewBag data for error scenarios
        private async Task ReloadViewBagData()
        {
            try
            {
                var trainingsWithTests = await _context.TrainingPrograms
                    .Where(t => t.GoogleFormTestLink != null && t.RecStatus == "active")
                    .Select(t => new { t.TrainingSysID, t.Title })
                    .ToListAsync();

                ViewBag.Trainings = trainingsWithTests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading ViewBag data");
                ViewBag.Trainings = new List<object>();
            }
        }
    }
}
