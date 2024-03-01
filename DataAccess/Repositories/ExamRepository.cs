using Dapper;
using DataAccess.Entities;
using DataAccess.Entities.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
   public  class ExamRepository:GenericRepository<Exam>
    {
        public async Task<IEnumerable<ExamView>> GetTeacherExams(long TeacherId,long? CourseId,bool? Publish, int Page)
        {
            Page = Page * 30;
            var sql = @"select Exam.*,Track.Name as TrackName,Track.NameLT as TrackNameLT,Course.Name as CourseName,Course.NameLT as CourseNameLT,
ExamType.Name as ExamTypeName,ExamType.NameLT as ExamTypeNameLT,[Subject].Name as SubjectName,[Subject].NameLT as SubjectNameLT,
(select count(StudentCourse.StudentId) from StudentCourse where CourseId=Exam.CourseId) As  NumberOfStudent,
(select Count(Id) from ExamQuestion where ExamId=Exam.Id) as QuestionsCount
 from Exam join Course on Exam.CourseId=Course.Id
join Track on Course.TrackId =Track.Id
join [Subject] on  [Subject].Id=Track.SubjectId
join ExamType on Exam.ExamTypeId=ExamType.Id where Track.TeacherId=" + TeacherId + "";
            if (CourseId>0)
            {
                sql = sql + " And Exam.CourseId=" + CourseId + "";
            }
            if (Publish!=null)
            {
                sql = sql + " And Exam.Publish='" + Publish + "'";

            }
            sql =sql+" order by Exam.Id desc  OFFSET " + Page + " Rows FETCH Next 30 Rows ONLY";
            var Exams = await _connectionFactory.GetConnection.QueryAsync<ExamView>(sql);
            return Exams;
        }


        public async Task<IEnumerable<StudentExamsToCorrect>> GetStudentExamsToCorrect(long examId, int Page,long? solveStatusId)
        {
            var parameters = new {  ExamId = examId, SolveStatusId= solveStatusId };

            Page = Page * 30;
            var sql = @"select * from ( select distinct StudentCourse.*,Student.Name as StudentName,Course.Name as CourseName,Course.NameLT as CourseNameLT,
Track.Name as TrackName,Track.NameLT as TrackNameLT,Exam.Id as ExamId,Exam.Name as ExamName,
IsNull( (select top 1 1 from StudentExam where StudentId=StudentCourse.StudentId and StudentExam.ExamId=Exam.Id ) ,0) as Done,
IsNull( (select top 1  SolveStatusId from StudentExam where StudentId=StudentCourse.StudentId and StudentExam.ExamId=Exam.Id) ,0) as SolveStatusId,
IsNull((select top 1 Name from SolveStatus where Id=(select Top 1 SolveStatusId from StudentExam where StudentId=StudentCourse.StudentId  and StudentExam.ExamId=Exam.Id))  ,'Not Solved')as SolveStatusName,
IsNull((select top 1 NameLT from SolveStatus where Id=(select Top 1  SolveStatusId from StudentExam where StudentId=StudentCourse.StudentId and StudentExam.ExamId=Exam.Id)),N'لم يحل') as SolveStatusNameLT,
IsNull((select top 1 Color from SolveStatus where Id=(select Top 1 SolveStatusId from StudentExam where StudentId=StudentCourse.StudentId and StudentExam.ExamId=Exam.Id)),N'#f90a0a') as SolveStatusColor,
(case when  IsNull( (select Top 1 SolveStatusId from StudentExam where StudentId=StudentCourse.StudentId and StudentExam.ExamId=Exam.Id) ,0)=0
then 
    (select Top 1 'Lost' from Exam InsideExam where InsideExam.Id=Exam.Id and InsideExam.StartDate<(getutcdate())) 
   else 
   ''
   end) as DeadlineDateStatus
   ,
   (case when  IsNull( (select Top 1 SolveStatusId from StudentExam where StudentId=StudentCourse.StudentId and ExamId=@ExamId) ,0)=0
then 
    (select Top 1 N'تأخرت في تسليمه' from Exam InsideExam where InsideExam.Id=Exam.Id and InsideExam.StartDate<(getutcdate())) 
   else 
   ''
   end) as DeadlineDateStatusLT

 from StudentCourse 
join Exam  on Exam.CourseId=StudentCourse.CourseId
join Student on Student.Id=StudentCourse.StudentId
join Course on Course.Id=StudentCourse.CourseId
join Track on Course.TrackId=Track.Id) as outerTable where 1=1";
            if (solveStatusId!=null)
            {
                sql = sql + " and SolveStatusId = @SolveStatusId";
            }
            sql = sql + " and  ExamId=@ExamId order by ExamId  OFFSET " + Page + " Rows FETCH Next 30 Rows ONLY";
            var StudentExams =await _connectionFactory.GetConnection.QueryAsync<StudentExamsToCorrect>(sql, parameters);

            
            return StudentExams;

        }

        public async Task<IEnumerable<ExaminationSummary>>GetExaminationSummary(long studentExamId, long examId)
        {
            var sql = @"  select ExamQuestionTypeId,Name,NameLT,SUm(TotalQuestionDegree) as TotalQuestionDegree ,Sum(TotalAnswerDegree) as TotalAnswerDegree,sum(QuestionCount) as QuestionCount ,max(CorrectedByTeacher) as CorrectedByTeacher  from (
	select  ExamQuestion.ExamQuestionTypeId,ExamQuestionType.Name,ExamQuestionType.NameLT,Count(ExamQuestionTypeId) QuestionCount,sum(ExamQuestion.Degree ) as TotalQuestionDegree,0 as TotalAnswerDegree, 0 as CorrectedByTeacher from ExamQuestion 
	join ExamQuestionType on ExamQuestion.ExamQuestionTypeId=ExamQuestionType.Id where ExamId=" + examId+" "+
    @"  group by  ExamQuestion.ExamQuestionTypeId,ExamQuestionType.Name,ExamQuestionType.NameLT
	union
	select  StudentAnswer.ExamQuestionTypeId,ExamQuestionType.Name,ExamQuestionType.NameLT,0 as QuestionCount,0 as TotalQuestionDegree,sum(StudentAnswer.Degree ) as TotalAnswerDegree ,( select Count(SA.Corrected) from  StudentAnswer as SA where SA.StudentExamId= " + studentExamId + " and SA.ExamQuestionTypeId=StudentAnswer.ExamQuestionTypeId and SA.Corrected=1 )  as CorrectedByTeacher " +
@"
    from StudentAnswer 
	join ExamQuestionType on StudentAnswer.ExamQuestionTypeId=ExamQuestionType.Id where StudentExamId=" + studentExamId + " "+
@"    group by  StudentAnswer.ExamQuestionTypeId,ExamQuestionType.Name,ExamQuestionType.NameLT) as VirtualTable
	group by  ExamQuestionTypeId,Name,NameLT ";
            var examinationSummary = await _connectionFactory.GetConnection.QueryAsync<ExaminationSummary>(sql);
            return examinationSummary;
        }

        public async Task<ExamView> GetExamDeatailsForTeacher(long ExamId)
        {
            var sql = @"select Exam.*,Track.Name as TrackName,Track.NameLT as TrackNameLT,Course.Image as CourseImage,Course.Name as CourseName,Course.NameLT as CourseNameLT,
ExamType.Name as ExamTypeName,ExamType.NameLT as ExamTypeNameLT,[Subject].Name as SubjectName,[Subject].NameLT as SubjectNameLT,
(select count(StudentCourse.StudentId) from StudentCourse where CourseId=Exam.CourseId) As  NumberOfStudent,
(select Count(ExamQuestion.Id) from ExamQuestion where ExamId=Exam.Id) as QuestionsCount
 from Exam join Course on Exam.CourseId=Course.Id
join Track on Course.TrackId =Track.Id
join [Subject] on  [Subject].Id=Track.SubjectId
join ExamType on Exam.ExamTypeId=ExamType.Id where Exam.Id=" + ExamId + "";
            var Exams = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<ExamView>(sql);
            return Exams;
        }

        public async Task<ExamView> GetExamDeatailsForStudent(long studentId,long examId)
        {
            var parameters = new { StudentId = studentId, ExamId = examId };
            var sql = @"select Exam.*,Track.Name as TrackName,Track.NameLT as TrackNameLT,Course.Image as CourseImage,Course.Name as CourseName,Course.NameLT as CourseNameLT,
ExamType.Name as ExamTypeName,ExamType.NameLT as ExamTypeNameLT,[Subject].Name as SubjectName,[Subject].NameLT as SubjectNameLT,
Teacher.Name as TeacherName,
(select Count(ExamQuestion.Id) from ExamQuestion where ExamId=Exam.Id) as QuestionsCount,
IsNull( (select SolveStatusId from StudentExam where StudentId=@StudentId and ExamId=@ExamId) ,0) as SolveStatusId,
IsNull((select Name from SolveStatus where Id=(select SolveStatusId from StudentExam where StudentId=@StudentId and ExamId=@ExamId))  ,'Not Solved')as SolveStatusName,
IsNull((select NameLT from SolveStatus where Id=(select SolveStatusId from StudentExam where StudentId=@StudentId and ExamId=@ExamId)),N'لم يحل') as SolveStatusNameLT,
IsNull((select Color from SolveStatus where Id=(select SolveStatusId from StudentExam where StudentId=@StudentId and ExamId=@ExamId)),N'#f90a0a') as SolveStatusColor,
(case when  IsNull( (select SolveStatusId from StudentExam where StudentId=@StudentId and ExamId=@ExamId) ,0)=0
then 
    (select 'Lost' from Exam InsideExam where InsideExam.Id=Exam.Id and InsideExam.StartDate<(getutcdate())) 
   else 
   ''
   end) as DeadlineDateStatus
   ,
   (case when  IsNull( (select SolveStatusId from StudentExam where StudentId=@StudentId and ExamId=@ExamId) ,0)=0
then 
    (select N'تأخرت في تسليمه' from Exam InsideExam where InsideExam.Id=Exam.Id and InsideExam.StartDate<(getutcdate())) 
   else 
   ''
   end) as DeadlineDateStatusLT

 from Exam join Course on Exam.CourseId=Course.Id
join Track on Course.TrackId =Track.Id
join [Subject] on  [Subject].Id=Track.SubjectId
join Teacher on Track.TeacherId=Teacher.Id
join ExamType on Exam.ExamTypeId=ExamType.Id where Exam.Id=@ExamId";
            var ExamData = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<ExamView>(sql,parameters);
            return ExamData;
        }

        public async Task<Exam> getExamByStudentExamId(long studentExamId)
        {
            var examdata = await _connectionFactory.GetConnection.QueryFirstOrDefaultAsync<Exam>("select * from Exam join StudentExam on Exam.Id=StudentExam.ExamId where StudentExam.Id=" + studentExamId + "");
            return examdata;
        }
    }
}
