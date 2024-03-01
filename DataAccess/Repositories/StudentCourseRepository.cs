using Dapper;
using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class StudentCourseRepository : GenericRepository<StudentCourse>
    {
        public async Task<int> GetCourseCountByStudentId(long StudentId)
        {
            var result = await GetAll(" where StudentId=" + StudentId + "");

            return result.Count();
        }

        public async Task<bool> CheckIfStudentEnrolledInThisCourseBefore(long CourseId, long StudentId)
        {
            var result = await GetWhere(" where StudentId=" + StudentId + " And CourseId=" + CourseId + " ");
            if (result == null)
            {
                return false;
            }
            return true;
        }

        public async Task<bool> CheckIfStudentEnrolledInThisLiveBefore(long liveId, long StudentId)
        {
            var result = await GetWhere(" where StudentId=" + StudentId + " And CourseId=" + liveId + " ");
            if (result == null)
            {
                return false;
            }
            return true;
        }



        public async Task<IEnumerable<MyCourse>> GetMyCourses(long StudentId, int Page)
        {
            try
            {
                Page = Page * 30;
                var conn = _connectionFactory.GetConnection;
                string queryGetHeader = @"select distinct * from 
            (
            select SubCategory.Name as SubCategoryName ,SubCategory.NameLT as SubCategoryNameLT
                        ,[Subject].Id as SubjectId,[Subject].Name as SubjectName,[Subject].NameLT as SubjectNameLT
                        from Subject join Track on Subject.Id=Track.SubjectId
                        join Department on Department.Id=Subject.DepartmentId 
                        join SubCategory on SubCategory.Id=Department.SubCategoryId 
                        join Course on Course.TrackId=Track.Id
            where Course.Id In (select Distinct CourseId from StudentCourse where StudentId=" + StudentId + ")) NewTable order by NewTable.SubjectId OFFSET " + Page + " Rows FETCH Next 30 Rows ONLY";
                var Header = await conn.QueryAsync<MyCourse>(queryGetHeader);

                foreach (var item in Header)
                {
string sqlBody = @"select myCourse.* , (select Name from Teacher where Id=Track.TeacherId) as TeacherName,
(select  Count(StudentContent.Id)  from Content left Join StudentContent  on Content.Id=StudentContent.ContentId 
left join  [Group] on [Group].Id=Content.GroupId left join Course on [Group].CourseId=Course.Id where Course.Id=myCourse.Id  " +
" And StudentContent.StudentId="+StudentId+") ViewedContent," +
"(select  Count(Content.Id)  from Content  left join  [Group] on [Group].Id=Content.GroupId "+
" left join Course on [Group].CourseId=Course.Id join StudentCourse on StudentCourse.CourseId=Course.Id  where Course.Id=myCourse.Id   And StudentCourse.StudentId="+StudentId+")"+
@" as ContentCount from Subject join Track on Subject.Id=Track.SubjectId 
join Department on Department.Id=Subject.DepartmentId  
join SubCategory on SubCategory.Id=Department.SubCategoryId 
join Course as myCourse on myCourse.TrackId=Track.Id            
where myCourse.CourseStatusId=3 And myCourse.Id In (select Distinct CourseId from StudentCourse where StudentId=" + StudentId+"" +
" And CourseId Not IN (select Course.Id from TrackSubscription join Course on TrackSubscription.TrackId=Course.TrackId where TrackSubscription.TrackId=Track.Id And StudentId="+StudentId+" and DurationExpiration< getdate()) ) And [Subject].Id=" + item.SubjectId+"";
                    var Body = await conn.QueryAsync<Course>(sqlBody);
                    if (Body.Count() > 0)
                    {
                        item.Courses = Body;
                    }


                }

                return Header;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<IEnumerable<Student>> GetAllStudentForRenewSubscription()
        {
            var Students = await _connectionFactory.GetConnection.QueryAsync<Student>(@"select Student.* from TrackSubscription
 join Student on Student.Id=TrackSubscription.StudentId
 where DATEDIFF(DAY,getdate(),DurationExpiration)=3");

            return Students;
        }
    }
}
