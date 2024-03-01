using Dapper;
using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
   public class FavouriteRepository:GenericRepository<Favourite>
    {
        public async Task<bool> IsAdded(Favourite favourite)
        {
            var IsFound = await GetAll(" Where CourseId="+favourite.CourseId+" And StudentId="+favourite.StudentId+" ");
            if (IsFound.Count()>0)
            {
                return true;
            }
            return false;
        }
        public async Task<Favourite> GetFavourite(Favourite favourite)
        {
            var result = await GetWhere(" Where CourseId=" + favourite.CourseId + " And StudentId=" + favourite.StudentId + " ");
           
            return result;
        }
        public async Task<IEnumerable<Course>> GetAllFavourite(long StudentId, int Page)
        {
            Page = Page * 30;
            string sql= @"select Course.*,Teacher.Name As TeacherName from Course join Favourite on Favourite.CourseId=Course.Id join Track on Track.Id=Course.TrackId Join Teacher on Track.TeacherId=Teacher.Id
                        where Favourite.StudentId=" + StudentId+"  order by Course.Id OFFSET " + Page + " Rows FETCH Next 30 Rows ONLY";
            var result =await _connectionFactory.GetConnection.QueryAsync<Course>(sql);

            return result;
        }
    }
}
