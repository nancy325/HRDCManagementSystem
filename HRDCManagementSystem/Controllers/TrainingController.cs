using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HRDCManagementSystem.Data;
using HRDCManagementSystem.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HRDCManagementSystem.Models.Entities;

namespace HRDCManagementSystem.Controllers
{   
    public class TrainingController : Controller
    {
        private readonly HRDCContext _context;

        public TrainingController(HRDCContext context)
        {
            _context = context;
        }
        // GET: TrainingController
        [HttpGet]
        public ActionResult TrainingIndex()
        {
            var tarinings = _context.TrainingPrograms.Where(t => t.RecStatus == "active").ToList();
            return View(tarinings);
        }

        // GET: TrainingController/Details/5
        [HttpGet]
        public ActionResult Details(int id)
        {
            var training = _context.TrainingPrograms.FirstOrDefault(t => t.TrainingSysID == id);
            if (training == null)
            {
                return NotFound();
            }
            return View(training);
        }

        // GET: TrainingController/Create
        [HttpGet]
        public ActionResult CreateTraining()
        {
            return View();
        }

        // POST: TrainingController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("Admin")]
        public ActionResult CreateTraining(TrainingProgram model)
        {
            if (ModelState.IsValid) {
                model.CreateDateTime = DateTime.Now;
                _context.TrainingPrograms.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(TrainingIndex));
            }
            return View(model);
        }

        // GET: TrainingController/Edit/5
        [HttpGet]
        [Authorize("Admin")]
        public ActionResult EditTraining(int id)
        {
            var training = _context.TrainingPrograms.FirstOrDefault(t => t.TrainingSysID == id && t.RecStatus == "active");
            if (training == null)
                return NotFound();
            return View(training);
        }

        // POST: TrainingController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTraining(int id, TrainingProgram model)
        {
            if (id != model.TrainingSysID) {
                return BadRequest();
            }
            if (ModelState.IsValid) {
                var exisiting = _context.TrainingPrograms.Find(id);
                if(exisiting == null)
                    return BadRequest();
                model.ModifiedDateTime = DateTime.Now;
                model.CreateDateTime = exisiting.CreateDateTime; //preserve original
                _context.Entry(exisiting).CurrentValues.SetValues(model);   
                _context.SaveChanges();
                return RedirectToAction(nameof(TrainingIndex));
            }
            return View(model);
        }

        // GET: TrainingController/Delete/5
        [HttpGet]
        [Authorize("Admin")]
        public ActionResult DeleteTraining(int id)
        {
            var training = _context.TrainingPrograms.Find(id);
            if (training == null)
                return NotFound();

            return View(training);

        }

        // POST: TrainingController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize("Admin")]
        public ActionResult ConfirmDelete(int id)
        {
            var training = _context.TrainingPrograms.Find(id);
            if (training == null)
                return NotFound();

            training.RecStatus = "inactive";
            training.ModifiedDateTime = DateTime.Now;
            _context.SaveChanges();

            return RedirectToAction(nameof(TrainingIndex));
        }
    }
}
