//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using HRDCManagementSystem.Models.Entities;
//using HRDCManagementSystem.Data;
//using Microsoft.AspNetCore.Authorization;
//using HRDCManagementSystem.Models.Request;

//namespace HRDCManagementSystem.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class EmployeeController : ControllerBase
//    {
//        private readonly IDbContextFactory<HRDCContext> _contextFactory;

//        public EmployeeController(IDbContextFactory<HRDCContext> contextFactory)
//        {
//            _contextFactory = contextFactory;
//        }

//        // ✅ Get all employees
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<Employee>>> GetAllEmployees()
//        {
//            using var context = _contextFactory.CreateDbContext();

//            var employees = await (from e in context.Employees.AsNoTracking()
//                                   where e.RecStatus == "A"
//                                   select e).ToListAsync();

//            return Ok(employees);
//        }

//        // ✅ Get employee by ID
//        [HttpGet("{id}")]
//        public async Task<ActionResult<Employee>> GetEmployeeById(int id)
//        {
//            using var context = _contextFactory.CreateDbContext();

//            var employee = await (from e in context.Employees.AsNoTracking()
//                                  where e.EmployeeSysID == id
//                                  select e).FirstOrDefaultAsync();

//            if (employee == null)
//                return NotFound();

//            return Ok(employee);
//        }

//        // ✅ Add new employee (Admin only)
//        [HttpPost]
//        [Authorize(Roles = "Admin")]
//        public async Task<ActionResult<Employee>> AddEmployee(EmployeeReq employee)
//        {
//            using var context = _contextFactory.CreateDbContext();

//            employee.CreateDateTime = DateTime.Now;
//            employee.RecStatus = "A";

//            context.Employees.Add(employee);
//            await context.SaveChangesAsync();

//            return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.EmployeeSysID }, employee);
//        }

//        // ✅ Update employee
//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
//        {
//            if (id != employee.EmployeeSysID)
//                return BadRequest();

//            using var context = _contextFactory.CreateDbContext();

//            employee.ModifiedDateTime = DateTime.Now;

//            context.Attach(employee);
//            context.Entry(employee).State = EntityState.Modified;

//            try
//            {
//                await context.SaveChangesAsync();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                if (!context.Employees.Any(e => e.EmployeeSysID == id))
//                    return NotFound();
//                else
//                    throw;
//            }

//            return NoContent();
//        }

//        // ✅ Soft delete employee (Admin only)
//        [HttpDelete("{id}")]
//        [Authorize(Roles = "Admin")]
//        public async Task<IActionResult> DeleteEmployee(int id)
//        {
//            using var context = _contextFactory.CreateDbContext();

//            var employee = await context.Employees.AsNoTracking()
//                .FirstOrDefaultAsync(e => e.EmployeeSysID == id);

//            if (employee == null)
//                return NotFound();

//            employee.RecStatus = "D";
//            employee.ModifiedDateTime = DateTime.Now;

//            context.Attach(employee);
//            context.Entry(employee).Property(e => e.RecStatus).IsModified = true;
//            context.Entry(employee).Property(e => e.ModifiedDateTime).IsModified = true;

//            await context.SaveChangesAsync();

//            return NoContent();
//        }
//    }
//}
