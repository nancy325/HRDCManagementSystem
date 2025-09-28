using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace HRDCManagementSystem.Controllers
{
    public class TrainingController : Controller
    {
        private readonly HRDCContext _context;

        public TrainingController(HRDCContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ActionName("TrainingIndex")]
        public async Task<ActionResult> TrainingIndex()
        {
            var trainings = await _context.TrainingPrograms
                .Where(t => t.RecStatus == "active")
                .ToListAsync();

            var viewModels = trainings.Select(MapToViewModel).ToList();
            return View(viewModels);
        }

        [HttpGet]
        [ActionName("Details")]
        public async Task<ActionResult> DetailsTraining(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");

            if (training == null)
            {
                return NotFound();
            }

            var viewModel = MapToViewModel(training);

            // If there is a file, check if it exists on disk and pass its relative URL to the view
            if (!string.IsNullOrEmpty(training.FilePath))
            {
                // Ensure the file path is a web path (starts with /)
                var relativePath = training.FilePath.StartsWith("/")
                    ? training.FilePath
                    : "/" + training.FilePath;

                // Map the relative path to the physical path
                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));

                if (System.IO.File.Exists(physicalPath))
                {
                    viewModel.ExistingPath = relativePath;
                }
                else
                {
                    // File does not exist, so do not provide a link
                    viewModel.ExistingPath = null;
                    ViewBag.FileError = "The uploaded file could not be found or is unavailable.";
                }
            }
            else
            {
                viewModel.ExistingPath = null;
            }

            // Check if current user is registered for this training (for employees)
            if (User.IsInRole("Employee"))
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserEmail))
                {
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.UserSys.Email == currentUserEmail && e.RecStatus == "active");

                    if (employee != null)
                    {
                        var isRegistered = await _context.TrainingRegistrations
                            .AnyAsync(tr => tr.EmployeeSysID == employee.EmployeeSysID && 
                                          tr.TrainingSysID == id && 
                                          tr.RecStatus == "active");
                        ViewBag.IsRegistered = isRegistered;
                    }
                }
            }

            // Get current registration count
            var currentRegistrations = await _context.TrainingRegistrations
                .CountAsync(tr => tr.TrainingSysID == id && tr.RecStatus == "active");
            ViewBag.CurrentRegistrations = currentRegistrations;

            return View("Details", viewModel);
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateTraining()
        {
            return View("CreateTraining", new TrainingViewModel());
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View("CreateTraining", new TrainingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [ActionName("Create")]
        public async Task<ActionResult> CreateTrainingTraining(TrainingViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateTraining", viewModel);
            }

            string uniqueFileName = null;
            if (viewModel.FilePath != null && viewModel.FilePath.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.FilePath.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.FilePath.CopyToAsync(stream);
                }
            }

            var entity = MapToEntity(viewModel);
            entity.FilePath = uniqueFileName != null ? "/uploads/" + uniqueFileName : null;

            _context.TrainingPrograms.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TrainingIndex));
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ActionName("EditTraining")]
        public async Task<ActionResult> EditTrainingTraining(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
                return NotFound();

            var viewModel = MapToViewModel(training);
            ViewData["TrainingSysID"] = id;
            return View("EditTraining", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [ActionName("EditTraining")]
        public async Task<IActionResult> EditTrainingTraining(int id, TrainingViewModel model)
        {
            if (id != model.TrainingSysID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View("EditTraining", model);
            }

            try
            {
                var entity = await _context.TrainingPrograms.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                // Map non-file properties
                ApplyViewModelToEntity(entity, model);

                // Handle file upload
                if (model.FilePath != null && model.FilePath.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FilePath.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.FilePath.CopyToAsync(stream);
                    }

                    entity.FilePath = "/uploads/" + uniqueFileName;
                }
                // If no new file is uploaded, keep the existing file path from the database
                // (entity.FilePath already contains the existing value, so no need to change it)

                _context.Update(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Training updated successfully!";
                return RedirectToAction(nameof(TrainingIndex));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unable to update training. Error: {ex.Message}");
                return View("EditTraining", model);
            }
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ActionName("DeleteTraining")]
        public async Task<ActionResult> DeleteTrainingTraining(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
                return NotFound();

            return View("DeleteTraining", MapToViewModel(training));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [ActionName("ConfirmDelete")]
        public async Task<ActionResult> ConfirmDeleteTraining(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
                return NotFound();

            training.RecStatus = "inactive";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TrainingIndex));
        }

        private static TrainingViewModel MapToViewModel(TrainingProgram tp)
        {
            return new TrainingViewModel
            {
                TrainingSysID = tp.TrainingSysID,
                Title = tp.Title,
                TrainerName = tp.TrainerName,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                FromTime = tp.fromTime,
                ToTime = tp.toTime,
                ValidTill = tp.Validtill ?? tp.EndDate,
                Venue = tp.Venue,
                EligibilityType = tp.EligibilityType,
                Capacity = tp.Capacity,
                ExistingPath = tp.FilePath,
                Mode = tp.Mode,
                Status = tp.Status,
                MarksOutOf = tp.MarksOutOf,
                IsMarksEntry = tp.IsMarksEntry
            };
        }

        private static TrainingProgram MapToEntity(TrainingViewModel vm)
        {
            return new TrainingProgram
            {
                TrainingSysID = vm.TrainingSysID,
                Title = vm.Title,
                TrainerName = vm.TrainerName,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                fromTime = vm.FromTime,
                toTime = vm.ToTime,
                Validtill = vm.ValidTill,
                Venue = vm.Venue,
                EligibilityType = vm.EligibilityType,
                Capacity = vm.Capacity,
                Mode = vm.Mode,
                Status = vm.Status,
                MarksOutOf = vm.MarksOutOf,
                IsMarksEntry = vm.IsMarksEntry
            };
        }

        private static void ApplyViewModelToEntity(TrainingProgram entity, TrainingViewModel vm)
        {

            entity.Title = vm.Title;
            entity.TrainerName = vm.TrainerName;
            entity.StartDate = vm.StartDate;
            entity.EndDate = vm.EndDate;
            entity.fromTime = vm.FromTime;
            entity.toTime = vm.ToTime;
            entity.Validtill = vm.ValidTill;
            entity.Venue = vm.Venue;
            entity.EligibilityType = vm.EligibilityType;
            entity.Capacity = vm.Capacity;
            entity.Mode = vm.Mode;
            entity.Status = vm.Status;
            entity.MarksOutOf = vm.MarksOutOf;
            entity.IsMarksEntry = vm.IsMarksEntry;
        }
    }
}
