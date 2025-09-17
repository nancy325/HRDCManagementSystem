using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HRDCManagementSystem.Data;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HRDCManagementSystem.Models.Entities;
using HRDCManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;


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
        public async Task<ActionResult> TrainingIndex()
        {
            var trainings = await _context.TrainingPrograms
                .Where(t => t.RecStatus == "active")
                .ToListAsync();

            var viewModels = trainings.Select(MapToViewModel).ToList();
            return View(viewModels);
        }

        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
            {
                return NotFound();
            }
            return View(MapToViewModel(training));
        }

        // Keep old route used by list page button
        [HttpGet]
        public ActionResult CreateTraining()
        {
            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View("CreateTraining", new TrainingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(TrainingViewModel viewModel)
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
        public async Task<ActionResult> EditTraining(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
                return NotFound();

            var viewModel = MapToViewModel(training);
            ViewData["TrainingSysID"] = id;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> EditTraining(int id, TrainingViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                ViewData["TrainingSysID"] = id;
                return View(viewModel);
            }

            var existing = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (existing == null)
                return NotFound();

            ApplyViewModelToEntity(existing, viewModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TrainingIndex));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTraining(int id)
        {
            var training = await _context.TrainingPrograms
                .FirstOrDefaultAsync(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
                return NotFound();

            return View(MapToViewModel(training));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult> ConfirmDelete(int id)
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
