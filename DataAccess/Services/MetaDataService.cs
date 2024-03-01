using DataAccess.Entities;
using DataAccess.Repositories;
using DataAccess.Services;
using DataAccess.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class MetaDataService
    {
     
        public MetaDataService()
        {
            
        }

        public async Task<MetaData> GetMetaData()
        {
            UnitOfWork.MetaDataUnit unitOfWork = new UnitOfWork.MetaDataUnit();

            MetaData metaData = new MetaData();
            var countries = await unitOfWork.CountryRepository.GetAll("");
            var paymentMethods = await unitOfWork.PaymentMethodRepository.GetAll("");
            var termAndConditions = await unitOfWork.TermAndConditionRepository.GetAll("");
            var references = await unitOfWork.ReferenceRepository.GetAll("");
            var courseStatuses = await unitOfWork.CourseStatusRepository.GetAll("");
           var aboutUs = await unitOfWork.AboutUsRepository.GetAll("");
            var settings = await unitOfWork.SettingRepository.GetAll("");
            var systemSettings = await unitOfWork.SystemSettingRepository.GetAll("");
            var examQuestionTypes = await unitOfWork.ExamQuestionTypeRepository.GetAll("");
            var examTypes = await unitOfWork.ExamTypeRepository.GetAll("");
            var solveStatuses = await unitOfWork.SolveStatusRepository.GetAll("");


            metaData.Countries = countries.ToList<Country>();
            metaData.PaymentMethods = paymentMethods.ToList<PaymentMethod>();
            metaData.TermAndConditions = termAndConditions.ToList<TermAndCondition>();
            metaData.References = references.ToList<Reference>();
            metaData.CourseStatuses = courseStatuses.ToList<CourseStatus>();
            metaData.AboutUs = aboutUs.ToList<AboutUs>();
            metaData.Settings = settings.ToList<Setting>();
            metaData.SystemSettings = systemSettings.ToList<SystemSetting>();
            metaData.ExamQuestionTypes = examQuestionTypes.ToList();
            metaData.ExamTypes = examTypes.ToList();
            metaData.SolveStatuses = solveStatuses.ToList();
            return metaData;

        }
        public async Task<MetaData> GetMetaDataIOS()
        {
            UnitOfWork.MetaDataUnit unitOfWork = new UnitOfWork.MetaDataUnit();

            MetaData metaData = new MetaData();
            var countries = await unitOfWork.CountryRepository.GetAll("");
            var paymentMethods = await unitOfWork.PaymentMethodRepository.GetAll("");
            var termAndConditions = await unitOfWork.TermAndConditionRepository.GetAll("");
            var references = await unitOfWork.ReferenceRepository.GetAll("");
            var courseStatuses = await unitOfWork.CourseStatusRepository.GetAll("");
            var aboutUs = await unitOfWork.AboutUsRepository.GetAll("");
            var settings = await unitOfWork.SettingRepository.GetAll("where Id != 4 and Id !=5 and Id !=6 and Id !=3");
            var systemSettings = await unitOfWork.SystemSettingRepository.GetAll("where Id !=1 and Id !=2 and Id !=3");
            var examQuestionTypes = await unitOfWork.ExamQuestionTypeRepository.GetAll("");
            var examTypes = await unitOfWork.ExamTypeRepository.GetAll("");
            var solveStatuses = await unitOfWork.SolveStatusRepository.GetAll("");


            metaData.Countries = countries.ToList<Country>();
            metaData.PaymentMethods = paymentMethods.ToList<PaymentMethod>();
            metaData.TermAndConditions = termAndConditions.ToList<TermAndCondition>();
            metaData.References = references.ToList<Reference>();
            metaData.CourseStatuses = courseStatuses.ToList<CourseStatus>();
            metaData.AboutUs = aboutUs.ToList<AboutUs>();
            metaData.Settings = settings.ToList<Setting>();
            metaData.SystemSettings = systemSettings.ToList<SystemSetting>();
            metaData.ExamQuestionTypes = examQuestionTypes.ToList();
            metaData.ExamTypes = examTypes.ToList();
            metaData.SolveStatuses = solveStatuses.ToList();
            return metaData;

        }

    }
}
