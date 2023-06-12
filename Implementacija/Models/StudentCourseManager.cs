using Microsoft.EntityFrameworkCore;
using ooadproject.Data;

namespace ooadproject.Models
{
    public class StudentCourseManager
    {
        private readonly ApplicationDbContext _context;  
        private readonly GradesManager _gradeManager;
        public StudentCourseManager(ApplicationDbContext context) {
            _context = context;
            _gradeManager = new GradesManager(_context);
        }
        public class StudentCourseInfo
        {               
                public StudentCourseInfo() { }
                public StudentCourse student;
                public double TotalPoints;
                public int numberOfPassed;
                public int Grade;
        }

        public GradesManager Get_gradeManager()
        {
            return _gradeManager;
        }

        public async Task<List<StudentCourseInfo>> RetrieveStudentCourseInfo(int? courseID)
        {
            var studentCourses = await _context.StudentCourse.Include(sc => sc.Student)
                                                             .Where(sc => sc.CourseID == courseID)
                                                             .ToListAsync();

            var results = new List<StudentCourseInfo>();

            foreach (var studentCourse in studentCourses)
            {
                var totalPoints = await GetTotalPoints(studentCourse.ID);
                var grade = EvaluateGrade(totalPoints);

                var studentCourseInfo = new StudentCourseInfo
                {
                    student = studentCourse,
                    TotalPoints = totalPoints,
                    Grade = grade
                };

                results.Add(studentCourseInfo);
            }

            return results;
        }

        public int GetNumberOfPassed(List<StudentCourseInfo> studentCourses)
        {
            return studentCourses.Count(sc => sc.Grade >= 6);
        }

        public int EvaluateGrade(double points)
        {
            if (points < 0)
                throw new ArgumentException("Points cannot be negative.");

            int grade;

            if (points >= 95)
                grade = 10;
            else if (points >= 85)
                grade = 9;
            else if (points >= 75)
                grade = 8;
            else if (points >= 65)
                grade = 7;
            else if (points >= 55)
                grade = 6;
            else
                grade = 0;

            return grade;
        }

        public async Task<double> GetTotalPoints(int courseId)
        {
            var exams = _context.StudentExam.Where(e => e.CourseID == courseId);
            var hworks = _context.StudentHomework.Where(e => e.CourseID == courseId);

            double total = exams.Sum(exam => exam.PointsScored) + hworks.Sum(hwork => hwork.PointsScored);

            return total;
        }



        public async Task<double> GetMaximumPoints(int? courseID)
        {
            var exams = await _context.Exam.Where(e => e.CourseID == courseID).ToListAsync();
            var hworks = await _context.Homework.Where(e => e.CourseID == courseID).ToListAsync();

            double totalExams = exams.Sum(exam => exam.TotalPoints);
            double totalHomeworks = hworks.Sum(hwork => hwork.TotalPoints);

            return totalExams + totalHomeworks;
        }


    }
}
