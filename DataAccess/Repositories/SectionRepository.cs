using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
   public class SectionRepository:GenericRepository<Section>
    {
        CategoryRepository _categoryRepository = new CategoryRepository();
        SubCategoryRepository _subCategoryRepository = new SubCategoryRepository();
        DepartmentRepository _departmentRepository=new DepartmentRepository();
        public async Task<IEnumerable<Section>> GetSectionsByCountryId(long CountryId,bool isIncludeSubCategory=false)
        {

            var Sections = await GetAll(" Where CountryId=" + CountryId + " ");
            foreach (var section in Sections)
            {
                var categories = await _categoryRepository.GetAll(" where SectionId=" + section.Id + "");
                section.Categories = categories;
                if (isIncludeSubCategory == true)
                {
                    foreach (var category in categories)
                    {
                        var subCategories = await _subCategoryRepository.GetAll(" where CategoryId=" + category.Id + "");
                        category.SubCategories = subCategories.ToList();
                        foreach (var subCategory in subCategories)
                        {
                            var departments = await _departmentRepository.GetAll(" where SubCategoryId=" + subCategory.Id + "");
                            subCategory.Departments = departments;
                        }
                    }
                    
                }
            }

            return Sections;

        }
    }
}
