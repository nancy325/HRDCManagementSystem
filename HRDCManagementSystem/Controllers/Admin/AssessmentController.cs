﻿using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDCManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AssessmentController : Controller
    {
        private readonly HRDCContext _context;

        public AssessmentController(HRDCContext context)
        {
            _context = context;
        }

        // Display all trainings with Google Form Tests
        [HttpGet]
        public async Task<IActionResult> Index(string search)
        {
            var trainingsQuery = _context.TrainingPrograms
                .Where(t => t.GoogleFormTestLink != null && t.RecStatus == "active");

            if (!string.IsNullOrEmpty(search))
            {
                trainingsQuery = trainingsQuery.Where(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var trainings = await trainingsQuery.ToListAsync();

            // Map entity -> viewmodel using Mapster
            var trainingsWithTests = trainings.Adapt<List<TrainingAssessmentViewModel>>();

            return View("~/Views/Admin/Assessment/Index.cshtml", trainingsWithTests);
        }

        // Show marks entry form for a training
        [HttpGet]
        public async Task<IActionResult> ManageMarks(int id)
        {
            var training = await _context.TrainingPrograms
                .Include(t => t.TrainingRegistrations)
                    .ThenInclude(r => r.EmployeeSys)
                .Include(t => t.TrainingRegistrations)
                    .ThenInclude(r => r.Attendances)
                .FirstOrDefaultAsync(t => t.TrainingSysID == id);

            if (training == null)
                return NotFound();

            var vm = new AssessmentMarksEntryViewModel
            {
                TrainingId = training.TrainingSysID,
                TrainingTitle = training.Title,
                MarksOutOf = training.MarksOutOf ?? 0,
                EmployeeMarks = training.TrainingRegistrations
                                    .Where(r => r.RecStatus == "active" && r.Registration == true && r.Confirmation == true)
                                    .Adapt<List<EmployeeMarksViewModel>>() // Map registrations -> EmployeeMarksViewModel
            };

            // Load all trainings with Google Form Tests for the dropdown
            var trainingsWithTests = await _context.TrainingPrograms
                .Where(t => t.GoogleFormTestLink != null && t.RecStatus == "active")
                .Select(t => new { t.TrainingSysID, t.Title })
                .ToListAsync();

            ViewBag.Trainings = trainingsWithTests;

            return View("~/Views/Admin/Assessment/ManageMarks.cshtml", vm);
        }

        // Save marks entered by admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageMarks(AssessmentMarksEntryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Assessment/ManageMarks.cshtml", model);
            }

            // Additional validation: Ensure marks do not exceed MarksOutOf
            foreach (var empMark in model.EmployeeMarks)
            {
                if (empMark.MarksObtained.HasValue && empMark.MarksObtained.Value > model.MarksOutOf)
                {
                    ModelState.AddModelError($"EmployeeMarks[{model.EmployeeMarks.IndexOf(empMark)}].MarksObtained", $"Marks obtained cannot exceed {model.MarksOutOf}.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Assessment/ManageMarks.cshtml", model);
            }

            foreach (var empMark in model.EmployeeMarks)
            {
                var registration = await _context.TrainingRegistrations
                                    .FirstOrDefaultAsync(r => r.TrainingRegSysID == empMark.RegistrationId);

                if (registration != null)
                {
                    // Map ViewModel -> Entity using Mapster
                    empMark.Adapt(registration);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Marks updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
