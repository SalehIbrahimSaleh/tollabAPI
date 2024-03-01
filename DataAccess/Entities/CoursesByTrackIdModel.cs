using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Entities
{
   public class CoursesByTrackIdModel
    {
        public string CategoryName { get; set; }
        public string CategoryNameLT { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryNameLT { get; set; }
        public string TrackName { get; set; }
        public string TrackNameLT { get; set; }
        public string TrackImage { get; set; }
        public string TeacherName { get; set; }
        public string TeacherPhoto { get; set; }
        public IEnumerable<Course> Courses { get; set; }
    }
}
