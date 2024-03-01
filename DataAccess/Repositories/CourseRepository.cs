using Dapper;
using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class CourseRepository : GenericRepository<Course>
    {
        public async Task<Course> GetCoursesById(long CourseId, long StudentId)
        {
            try
            {
                string query =
               @"select Distinct * from (
select OuterCourse.*,IsNull((select ShowWaterMark from Track where Id=OuterCourse.TrackId),'0' ) as TrackShowWaterMark ,IsNull((select '1' from Favourite where StudentId=" + StudentId + " And CourseId=OuterCourse.Id),'0' ) as IsFavourite, "+
"IsNull((select top 1 '1'  from StudentCourse where StudentId = " + StudentId+" And CourseId = OuterCourse.Id),'0' ) as Enrollment, "+
@"(select Count(Content.Id) from Content join [Group] on [Group].Id = Content.GroupId join Course on [Group].CourseId = Course.Id where Content.ContentTypeId = 1 and Course.Id = OuterCourse.Id) as VideoCount, 
(select Count(Content.Id) from Content join [Group] on [Group].Id = Content.GroupId join Course on [Group].CourseId = Course.Id where Content.ContentTypeId = 2 and Course.Id = OuterCourse.Id ) as FilesCount, 
(select IsNull(CAST(Sum(Content.Duration) as decimal) / 60, 0) from Content join [Group] on [Group].Id = Content.GroupId join Course on [Group].CourseId = Course.Id where Content.ContentTypeId = 1 and Course.Id = OuterCourse.Id ) as HoursCount,
(select Teacher.Name from Teacher join Track on Teacher.Id = Track.TeacherId join Course on Track.Id = Course.TrackId where Course.Id = OuterCourse.Id) as TeacherName ,
(select Teacher.Id from Teacher join Track on Teacher.Id = Track.TeacherId join Course on Track.Id = Course.TrackId where Course.Id = OuterCourse.Id) AS TeacherId
,(select BySubscription from Track join Course on  Track.Id=Course.TrackId where Course.Id=OuterCourse.Id) as BySubscription
 from Course as OuterCourse left join[Group] on OuterCourse.Id =[Group].CourseId left join Content On[Group].Id = Content.GroupId where OuterCourse.Id = " + CourseId + ") newTable";
                var CourseData = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<Course>(query);



                return CourseData;

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<Course> GetCoursesWithContentById(long CourseId, long StudentId,long ContentId)
        {
            try
            {
                string query =
               @"select Distinct * from (
select OuterCourse.* ,IsNull((select '1' from Favourite where StudentId=" + StudentId + " And CourseId=OuterCourse.Id),'0' ) as IsFavourite, " +
"IsNull((select top 1 '1'  from StudentCourse where StudentId = " + StudentId + " And CourseId = OuterCourse.Id),'0' ) as Enrollment, " +
@"(select Count(Content.Id) from Content join [Group] on [Group].Id = Content.GroupId join Course on [Group].CourseId = Course.Id where Content.ContentTypeId = 1 and Course.Id = OuterCourse.Id) as VideoCount, 
(select Count(Content.Id) from Content join [Group] on [Group].Id = Content.GroupId join Course on [Group].CourseId = Course.Id where Content.ContentTypeId = 2 and Course.Id = OuterCourse.Id ) as FilesCount, 
(select IsNull(CAST(Sum(Content.Duration) as decimal) / 60, 0) from Content join [Group] on [Group].Id = Content.GroupId join Course on [Group].CourseId = Course.Id where Content.ContentTypeId = 1 and Course.Id = OuterCourse.Id ) as HoursCount,
(select Teacher.Name from Teacher join Track on Teacher.Id = Track.TeacherId join Course on Track.Id = Course.TrackId where Course.Id = OuterCourse.Id) as TeacherName ,
(select Teacher.Id from Teacher join Track on Teacher.Id = Track.TeacherId join Course on Track.Id = Course.TrackId where Course.Id = OuterCourse.Id) AS TeacherId
,(select BySubscription from Track join Course on  Track.Id=Course.TrackId where Course.Id=OuterCourse.Id) as BySubscription
 from Course as OuterCourse left join[Group] on OuterCourse.Id =[Group].CourseId left join Content On[Group].Id = Content.GroupId where OuterCourse.Id = " + CourseId + ") newTable";
                var CourseData = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<Course>(query);
                if (CourseData != null)
                {
                    var ContentData = _connectionFactory.GetConnection.QueryFirstOrDefault<Content>("select * from Content where Id=" + ContentId + "");

                    CourseData.Content = ContentData;
                }


                return CourseData;

            }
            catch (Exception e)
            {

                throw e;
            }
        }


        public async Task<CoursesByTrackIdModel> GetCoursesByTrackId(long TrackId)
        {
            try
            {
                string query = @"select Category.Name as CategoryName,Category.NameLT as CategoryNameLT,SubCategory.Name SubCategoryName ,
                            SubCategory.NameLT SubCategoryNameLT
							,Track.Id as TrackId,Track.Name as TrackName,Track.NameLT as TrackNameLT,Track.Image as TrackImage,
                           (select Name from Teacher where Id=Track.TeacherId) as TeacherName,
                           (select Photo from Teacher where Id=Track.TeacherId) as TeacherPhoto
                           from Subject join Track on Subject.Id=Track.SubjectId
                           join Department on Department.Id=Subject.DepartmentId 
						   join SubCategory on SubCategory.Id=Department.SubCategoryId 
						   join Category on Category.Id=SubCategory.CategoryId
                           where Track.Id=" + TrackId + " ";
                var result = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<CoursesByTrackIdModel>(query);
                if (result != null)
                {
                    //                (select Count(StudentId) from StudentCourse where CourseId=OuterCourse.Id) CountStudentEnrolled,

                    var courses = await GetAllByQuery(@"
                select Distinct * from ( 
                select  OuterCourse.*,
              ( select Count(StudentCourse.StudentId) as StudentCount from StudentCourse join Course on  StudentCourse.CourseId=Course.Id join Track on Track.Id=Course.TrackId where StudentCourse.CourseId=OuterCourse.Id) as CountStudentEnrolled,
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id And OuterCourse.CourseStatusId=3) as VideoCount, 
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=2 and  Course.Id=OuterCourse.Id And OuterCourse.CourseStatusId=3 ) as FilesCount, 
                (select IsNull( CAST(Sum(Content.Duration) as decimal )/60,0) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id And OuterCourse.CourseStatusId=3) as HoursCount
                from Course as OuterCourse left join [Group] on [Group].CourseId=OuterCourse.Id where OuterCourse.TrackId=" + TrackId + " And OuterCourse.CourseStatusId=3  ) newTable  Order By OrderNumber");
                    result.Courses = courses;
                }
                return result;

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<CoursesByTrackIdModel> GetAllCoursesByTrackIdForTeacher(long TrackId)
        {
            try
            {
                string query = @"select Category.Name as CategoryName,Category.NameLT as CategoryNameLT,SubCategory.Name SubCategoryName ,
                            SubCategory.NameLT SubCategoryNameLT
							,Track.Id as TrackId,Track.Name as TrackName,Track.NameLT as TrackNameLT,Track.Image as TrackImage,
                         (select Name from Teacher where Id=Track.TeacherId) as TeacherName
                           from Subject join Track on Subject.Id=Track.SubjectId
                           join Department on Department.Id=Subject.DepartmentId 
						   join SubCategory on SubCategory.Id=Department.SubCategoryId 
						   join Category on Category.Id=SubCategory.CategoryId
                           where Track.Id=" + TrackId + " ";
                var result = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<CoursesByTrackIdModel>(query);
                if (result != null)
                {
                    //                (select Count(StudentId) from StudentCourse where CourseId=OuterCourse.Id) CountStudentEnrolled,

                    var courses = await GetAllByQuery(@"
                select Distinct * from ( 
                select  OuterCourse.*,
              ( select Count(StudentCourse.StudentId) as StudentCount from StudentCourse join Course on  StudentCourse.CourseId=Course.Id join Track on Track.Id=Course.TrackId where StudentCourse.CourseId=OuterCourse.Id) as CountStudentEnrolled,
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id And OuterCourse.CourseStatusId=3) as VideoCount, 
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=2 and  Course.Id=OuterCourse.Id And OuterCourse.CourseStatusId=3 ) as FilesCount, 
                (select IsNull( CAST(Sum(Content.Duration) as decimal )/60,0) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id And OuterCourse.CourseStatusId=3) as HoursCount
                from Course as OuterCourse left join [Group] on [Group].CourseId=OuterCourse.Id where OuterCourse.TrackId=" + TrackId + "   ) newTable  Order By OrderNumber");
                    result.Courses = courses;
                }
                return result;

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<CoursesByTrackIdModel> GetCoursesByTrackIdForTeacher(long TrackId)
        {
            try
            {
                string query = @"select Category.Name as CategoryName,Category.NameLT as CategoryNameLT,SubCategory.Name SubCategoryName ,
                            SubCategory.NameLT SubCategoryNameLT
							,Track.Id as TrackId,Track.Name as TrackName,Track.NameLT as TrackNameLT,Track.Image as TrackImage,
                         (select Name from Teacher where Id=Track.TeacherId) as TeacherName
                           from Subject join Track on Subject.Id=Track.SubjectId
                           join Department on Department.Id=Subject.DepartmentId 
						   join SubCategory on SubCategory.Id=Department.SubCategoryId 
						   join Category on Category.Id=SubCategory.CategoryId
                           where Track.Id=" + TrackId + "";
                var result = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<CoursesByTrackIdModel>(query);
                if (result != null)
                {
                    var courses = await GetAllByQuery(@"
              select Distinct * from ( select  OuterCourse.*,
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id And Course.CourseStatusId!=3) as VideoCount, 
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=2 and  Course.Id=OuterCourse.Id And Course.CourseStatusId!=3) as FilesCount, 
                (select IsNull( CAST(Sum(Content.Duration) as decimal )/60,0) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id And Course.CourseStatusId!=3 ) as HoursCount
                from Course as OuterCourse left join [Group] on [Group].CourseId=OuterCourse.Id where OuterCourse.TrackId=" + TrackId + " And OuterCourse.CourseStatusId!=3  ) newTable");
                    result.Courses = courses;
                }
                return result;

            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<List<Group>> GetGroupsWithContentsByCourseIdForStudent(long CourseId, long StudentId, int Page,long ContentId)
        {
            try
            {
                string AndContent = "";
                if (ContentId>0)
                {
                    AndContent = " And NewContent.Id=" + ContentId + "";
                }
                Page = Page * 3;
                if (Page > 0)
                {
                    var GroupList = new List<Group>();
                    return GroupList;
                }

                //    string sql1 = "select *,(select '1' from StudentContent where StudentContent.ContentId=Content.Id And StudentContent.StudentId=" + StudentId + ") as IsViewed  from  [Group]  join Content on Content.GroupId=[Group].Id  where [Group].CourseId=" + CourseId + "";
                string sql = @"
BEGIN
    DECLARE @enrolled INT;
    SELECT 
        @enrolled= Isnull(Student.Id,0)
    FROM
       Student  
        INNER JOIN StudentCourse   ON StudentCourse.StudentId = Student.Id where StudentCourse.StudentId="+StudentId+" And StudentCourse.CourseId="+CourseId+""+
       @" IF @enrolled > 0
    BEGIN
select [Group].*,NewContent.Id, NewContent.Path,NewContent.Name,NewContent.NameLT,NewContent.GroupId,NewContent.Duration,NewContent.ContentTypeId,'true' as IsFree ,(select  top 1 '1' from StudentContent where StudentContent.ContentId=NewContent.Id And StudentContent.StudentId=" + StudentId+") as IsViewed "+
 " from  [Group]  join Content as NewContent on NewContent.GroupId=[Group].Id where [Group].CourseId="+CourseId+ " Order By NewContent.OrderNumber " +
    @" END
    ELSE
    BEGIN
	select [Group].*,NewContent.Id, NewContent.Name,NewContent.NameLT,NewContent.GroupId,NewContent.Duration,NewContent.ContentTypeId,NewContent.IsFree,(select top 1 '1' from StudentContent where StudentContent.ContentId=NewContent.Id And StudentContent.StudentId=" + StudentId + ") as IsViewed,"+
    @"(select top 1 Content.Path from Content where IsFree=1 and GroupId=[Group].Id And Id=NewContent.Id ) as Path
  from  [Group]  join Content as NewContent on NewContent.GroupId=[Group].Id 
  where [Group].CourseId=" + CourseId + ""+AndContent+" Order By NewContent.OrderNumber " +
  @" END
    END";







                var GroupDictionary = new Dictionary<long, Group>();
                var list = _connectionFactory.GetConnection.Query<Group, Content, Group>(
                    sql,
                    (group, content) =>
                    {
                        Group groupEntry;

                        if (!GroupDictionary.TryGetValue(group.Id, out groupEntry))
                        {
                            groupEntry = group;
                            groupEntry.Contents = new List<Content>();
                            GroupDictionary.Add(groupEntry.Id, groupEntry);
                        }
                        groupEntry.Contents.Add(content);

                        return groupEntry;
                    },
                    splitOn: "Id")
                    .OrderBy(i=>i.OrderNumber)
                .Distinct()//.Skip(Page)
                .ToList();

                return list;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<IEnumerable<Course>> SearchAsync(string word,long studentCountry, int Page)
        {
            Page = Page * 30;

            string sql = @"select Teacher.Name as TeacherName, Course.* from Course join Track on 
            Track.Id=Course.TrackId join Teacher on Teacher.Id=Track.TeacherId
             where  Course.CourseStatusId=3 And Teacher.CountryId="+studentCountry+ " And ( Course.Name LIKE N'%" + word + "%' or Course.NameLT LIKE N'%" + word + "%' ) order by Course.Id  OFFSET " + Page + " Rows FETCH Next 30 Rows ONLY";
            var result = await GetAllByQuery(sql);
            return result;
        }

        public async Task<IEnumerable<Course>> GetCoursesByDepartmentId(long DepartmentId, int Page)
        {
            Page = Page * 30;

            string sql = @"select Teacher.Name as TeacherName,Course.* from Course join Track on Course.TrackId=Track.Id
 join Subject on Track.SubjectId=Subject.Id join Department on Subject.DepartmentId=Department.Id join Teacher on Teacher.Id=Track.TeacherId
 where Course.CourseStatusId=3 And Department.Id=" + DepartmentId + " order by Course.Id  OFFSET " + Page + " Rows FETCH Next 30 Rows ONLY";
            var result = await GetAllByQuery(sql);
            return result;
        }

        public async Task<List<Group>> GetGroupsWithContentsByCourseId(long CourseId, int Page,long ContentId)
        {
            try
            {
                Page = Page * 3;
                if (Page>0)
                {
                    var GroupList = new List<Group>();
                    return GroupList;
                }
                string sql = "select *  from  [Group] left join Content on Content.GroupId=[Group].Id  where [Group].CourseId=" + CourseId + " ";
                if (ContentId>0)
                {
                    sql = sql + " And Content.Id=" + ContentId + "";
                }
                sql = sql + " order by Content.OrderNumber";
                var GroupDictionary = new Dictionary<long, Group>();
                var list = _connectionFactory.GetConnection.Query<Group, Content, Group>(
                    sql,
                    (group, content) =>
                    {
                        Group groupEntry;

                        if (!GroupDictionary.TryGetValue(group.Id, out groupEntry))
                        {
                            groupEntry = group;
                            groupEntry.Contents = new List<Content>();
                            GroupDictionary.Add(groupEntry.Id, groupEntry);
                        }

                        groupEntry.Contents.Add(content);

                        return groupEntry;
                    },
                    splitOn: "Id")
                .Distinct()//.Skip(Page)
                .ToList();

                return list;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<Course> GetCourseByIdForTeacher(long courseId, long TeacherId)
        {

            
            string sql = @" select Distinct * from ( select  OuterCourse.*,
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id) as VideoCount, 
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=2 and  Course.Id=OuterCourse.Id ) as FilesCount, 
                (select IsNull( CAST(Sum(Content.Duration) as decimal )/60,0) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id ) as HoursCount
             , ( select Count(StudentCourse.StudentId) as StudentCount from StudentCourse join Course on  StudentCourse.CourseId=Course.Id join Track on Track.Id=Course.TrackId where StudentCourse.CourseId=OuterCourse.Id And Track.TeacherId=" + TeacherId + ") as CountStudentEnrolled " +
            " ,(select isnull( sum(Amount),0) from TeacherTransaction where CourseId=OuterCourse.Id And TeacherId=" + TeacherId + ") GainedMoney " +
             "  from Course as OuterCourse left join [Group] on [Group].CourseId=OuterCourse.Id where OuterCourse.Id=" + courseId + " ) newTable";
            var CourseData = await GetOneByQuery(sql);
            
            return CourseData;
        }

        public async  Task<Course> GetCourseByIdForTeacher(long courseId ,long TeacherId,long ContentId)
        {
            string sql = @" select Distinct * from ( select  OuterCourse.*,
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id) as VideoCount, 
                (select Count(Content.Id) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=2 and  Course.Id=OuterCourse.Id ) as FilesCount, 
                (select IsNull( CAST(Sum(Content.Duration) as decimal )/60,0) from Content join [Group] on [Group].Id=Content.GroupId join Course on [Group].CourseId=Course.Id where Content.ContentTypeId=1 and  Course.Id=OuterCourse.Id ) as HoursCount
             , ( select Count(StudentCourse.StudentId) as StudentCount from StudentCourse join Course on  StudentCourse.CourseId=Course.Id join Track on Track.Id=Course.TrackId where StudentCourse.CourseId=OuterCourse.Id And Track.TeacherId=" + TeacherId + ") as CountStudentEnrolled " +
            " ,(select isnull( sum(Amount),0) from TeacherTransaction where CourseId=OuterCourse.Id And TeacherId="+ TeacherId + ") GainedMoney "+
	         "  from Course as OuterCourse left join [Group] on [Group].CourseId=OuterCourse.Id where OuterCourse.Id="+ courseId + " ) newTable";
            var CourseData = await GetOneByQuery(sql);
            if (CourseData != null)
            {
                var ContentData = _connectionFactory.GetConnection.QueryFirstOrDefault<Content>("select * from Content where Id=" + ContentId + "");

                CourseData.Content = ContentData;
            }
            return CourseData;
        }


        public async Task<bool> SubmitToReview(long CourseId)
        {
            var oldCourse = await Get(CourseId);
            if (oldCourse!=null)
            {
                oldCourse.CourseStatusId = 2;//submit to review 
                var update = await Update(oldCourse);
                return update;
            }
            return false;
        }

        public async Task<Course> AddCourseFullDescription(Course course)
        {
            var oldCourse = await Get(course.Id);
            if (oldCourse != null)
            {
                oldCourse.FullDescription = course.FullDescription;
                var update = await Update(oldCourse);
                if (update)
                {
                    return oldCourse;
                }
            }
            return null;
        }

    }
}
