using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ooadproject.Data;
using ooadproject.Models;
using static ooadproject.Models.StudentCourseManager;

namespace ooadproject.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Person> _userManager;
        private readonly StudentCourseManager _courseManager;
        private readonly GradesManager _gradesManager;

        public CourseController(ApplicationDbContext context, UserManager<Person> userManager)
        {
            _context = context;
            _userManager = userManager;
            _courseManager = new StudentCourseManager(_context);
            _gradesManager = new GradesManager(_context);
        }

        public List<SelectListItem>  GetTeacherNamesList()
        {
            List<SelectListItem> Teachers = new List<SelectListItem>();

            foreach (var item in _context.Teacher)
            {
                Teachers.Add(new SelectListItem() { Text = $"{item.Title} {item.FirstName} {item.LastName}", Value = item.Id.ToString() });
            }

            return Teachers;

        }
        [Authorize(Roles = "StudentService")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Course.Include(c => c.Teacher);
            return View(await applicationDbContext.ToListAsync());
        }


        [Authorize(Roles = "StudentService")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Course == null)
            {
                return NotFound();
            }

            var course = await _context.Course
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        [Authorize(Roles = "StudentService")]
        public IActionResult Create()
        {
            // putting teacher names in list to display on Create form
            ViewData["TeacherID"] = new SelectList(GetTeacherNamesList(), "Value", "Text");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,TeacherID,AcademicYear,ECTS,Semester")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TeacherID"] = new SelectList(_context.Teacher, "Id", "Id", course.TeacherID);
            return View(course);
        }

        [Authorize(Roles = "StudentService")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Course == null)
            {
                return NotFound();
            }

            var course = await _context.Course.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["TeacherID"] = new SelectList(GetTeacherNamesList(), "Value", "Text");
            return View(course);
        }

        [Authorize(Roles = "StudentService")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,TeacherID,AcademicYear,ECTS,Semester")] Course course)
        {
            if (id != course.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TeacherID"] = new SelectList(_context.Teacher, "Id", "Id", course.TeacherID);
            return View(course);
        }
        [Authorize(Roles = "StudentService")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Course == null)
            {
                return NotFound();
            }

            var course = await _context.Course
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        [Authorize(Roles = "StudentService")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Course == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Course'  is null.");
            }
            var course = await _context.Course.FindAsync(id);
            if (course != null)
            {
                _context.Course.Remove(course);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
          return (_context.Course?.Any(e => e.ID == id)).GetValueOrDefault();
        }
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CourseStatus(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var courses = await _context.Course.Where(c => c.Teacher == user).ToListAsync();
            var course = await _context.Course.FindAsync(id);
            ViewData["course"] = course;
            ViewData["Courses"] = courses;
            /*
            
            var StudentCourses = await _context.StudentCourse.Where(sc => sc.Course == course).ToListAsync();
            var Students = new List<Student>();
            var user = await _userManager.GetUserAsync(User);
            var Courses = await _context.Course.Where(c => c.Teacher == user).ToListAsync();
            foreach (var item in StudentCourses)
            {
                Students.Add(item.Student);
            }

            var Exams = await _context.Exam.Where(e => e.Course == course).ToListAsync();

            var Homeworks = await _context.Homework.Where(h => h.Course == course).ToListAsync();

            
            ViewData["Courses"] = Courses;
            ViewData["StudentCourses"] = StudentCourses;
            ViewData["Students"] = Students;
            ViewData["Exams"] = Exams;
            ViewData["Homeworks"] = Homeworks;
            */
            var students = await _context.StudentCourse.Where(sc => sc.Course == course).ToListAsync();
            List<StudentCourseInfo> list = await _courseManager.RetrieveStudentCourseInfo(id);
            ViewData["Info"] = list;
            ViewData["Maximum"] =  await _courseManager.GetMaximumPoints(id);
            ViewData["NumberOfPassed"] = _courseManager.GetNumberOfPassed(list);
            ViewData["NumberOfStudents"] = students.Count;


            return View();
        }


    }
}
