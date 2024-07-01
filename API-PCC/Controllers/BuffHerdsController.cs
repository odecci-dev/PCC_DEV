using API_PCC.ApplicationModels;
using API_PCC.ApplicationModels.Common;
using API_PCC.Data;
using API_PCC.DtoModels;
using API_PCC.EntityModels;
using API_PCC.Manager;
using API_PCC.Models;
using API_PCC.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Protocol.Core.Types;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using static API_PCC.Manager.DBMethods;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Diagnostics.Metrics;
using static API_PCC.Controllers.UserController;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
namespace API_PCC.Controllers
{
    [Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class HBuffHerdsController : ControllerBase
    {
        DbManager db = new DbManager();
        DBMethods dbmet = new DBMethods();
        string status = "";
        private readonly PCC_DEVContext _context;

        public HBuffHerdsController(PCC_DEVContext context)
        {
            _context = context;
        }

        // POST: BuffHerds/search
        [HttpPost]
        public async Task<ActionResult<IEnumerable<HerdPagedModel>>> search(BuffHerdSearchFilterModel searchFilter)
        {
            validateDate(searchFilter);
            if (!searchFilter.sortBy.Field.IsNullOrEmpty())
            {
                if (searchFilter.sortBy.Field.ToLower().Equals("cowlevel"))
                {
                    searchFilter.sortBy.Field = "HerdSize";
                }
            }
            try
            {
                List<HBuffHerd> buffHerdList = await buildHerdSearchQuery(searchFilter).ToListAsync();
                var result = buildHerdPagedModel(searchFilter, buffHerdList);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(ex.GetBaseException().ToString());
            }
        }

        private IQueryable<HBuffHerd> buildHerdArchiveQuery(BuffHerdSearchFilterModel searchFilter)
        {
            IQueryable<HBuffHerd> query = _context.HBuffHerds;

            query = query
                .Include(herd => herd.buffaloType)
                .Include(herd => herd.feedingSystem);

            query = query.Where(herd => herd.DeleteFlag);

            return query;
        }

        private IQueryable<HBuffHerd> buildHerdSearchQuery(BuffHerdSearchFilterModel searchFilter)
        {
            IQueryable<HBuffHerd> query = _context.HBuffHerds;

            query = query
                .Include(herd => herd.buffaloType)
                .Include(herd => herd.feedingSystem).Where(a => !a.DeleteFlag);

            // assuming that you return all records when nothing is specified in the filter

            if (!searchFilter.searchValue.IsNullOrEmpty())
                query = query.Where(herd =>
                               herd.HerdCode.Contains(searchFilter.searchValue) ||
                               herd.HerdName.Contains(searchFilter.searchValue));
            if (!searchFilter.filterBy.Status.IsNullOrEmpty())
                query = query.Where(herd =>
                               herd.Status == int.Parse(searchFilter.filterBy.Status));
            if (!searchFilter.filterBy.BreedTypeCode.IsNullOrEmpty())
                query = query.Where(herd => herd.buffaloType.Any(buffaloType => buffaloType.BreedTypeCode.Equals(searchFilter.filterBy.BreedTypeCode)));

            if (!searchFilter.filterBy.HerdClassDesc.IsNullOrEmpty())
                query = query.Where(herd => herd.HerdClassDesc.Equals(searchFilter.filterBy.HerdClassDesc));

            if (!searchFilter.filterBy.feedingSystemCode.IsNullOrEmpty())
                query = query.Where(herd => herd.feedingSystem.Any(feedingSystem => feedingSystem.FeedingSystemCode.Equals(searchFilter.filterBy.feedingSystemCode)));

            if (!searchFilter.dateFrom.IsNullOrEmpty())
                query = query.Where(herd => herd.DateCreated >= DateTime.Parse(searchFilter.dateFrom));

            if (!searchFilter.dateTo.IsNullOrEmpty())
                query = query.Where(herd => herd.DateCreated <= DateTime.Parse(searchFilter.dateTo));


            if (!searchFilter.sortBy.Field.IsNullOrEmpty())
            {

                if (!searchFilter.sortBy.Sort.IsNullOrEmpty())
                {
                    query = query.OrderBy(searchFilter.sortBy.Field + " " + searchFilter.sortBy.Sort);
                }
                else
                {
                    query = query.OrderBy(searchFilter.sortBy.Field + " asc");

                }
            }
            else
            {
                query = query.OrderByDescending(herd => herd.Id);
            }

            return query;
        }
        // GET: BuffHerds/view/5
        [HttpGet("{herdCode}")]
        public async Task<ActionResult<BuffHerdViewResponseModel>> view(String herdCode)
        {
            var buffHerdModel = await _context.HBuffHerds
               .Include(herd => herd.buffaloType)
               .Include(herd => herd.feedingSystem)
               .Where(herd => !herd.DeleteFlag && herd.HerdCode.Equals(herdCode))
               .FirstOrDefaultAsync();

            if (buffHerdModel == null)
            {
                return Conflict("No records found!");
            }
            var viewResponseModel = populateViewResponseModel(buffHerdModel);
            return Ok(viewResponseModel);
        }

        // GET: BuffHerds/archive
        [HttpPost]
        public async Task<ActionResult<IEnumerable<HBuffHerd>>> archive(BuffHerdSearchFilterModel searchFilter)
        {
            List<HBuffHerd> buffHerdList = await buildHerdArchiveQuery(searchFilter).ToListAsync();

            var result = buildHerdPagedModel(searchFilter, buffHerdList);
            return Ok(result);
        }

        // PUT: BuffHerds/update/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> update(int id, BuffHerdUpdateModel registrationModel)
        {

            string filePath = @"C:\data\herdupdate.json"; // Replace with your desired file path



            dbmet.insertlgos(filePath, JsonSerializer.Serialize(registrationModel));

            DataTable buffHerdDataTable = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdSelectQueryById(), null, populateSqlParameters(id));

            if (buffHerdDataTable.Rows.Count == 0)
            {
                status = "No records matched!";
                return Conflict(status);
            }

            DataTable herdCLassificationRecord = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdClassificationSearchQueryByHerdClassDesc(), null, populateSqlParametersHerdClassDesc(registrationModel.HerdClassDesc));

            if (herdCLassificationRecord.Rows.Count == 0)
            {
                status = "No Herd Classification records matched!";
                return Conflict(status);
            }

            //var buffHerd = convertDataRowToHerdModel(buffHerdDataTable.Rows[0]);

            DataTable buffHerdDuplicateCheck = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdSelectDuplicateQueryByIdHerdNameHerdCode(), null, populateSqlParameters(id, registrationModel));

            // check for duplication
            if (buffHerdDuplicateCheck.Rows.Count > 0)
            {
                status = "Entity already exists";
                return Conflict(status);
            }

            var buffHerd = _context.HBuffHerds
                    .Single(x => x.Id == id);

            DataTable farmOwnerRecordsCheck = db.SelectDb_WithParamAndSorting(QueryBuilder.buildFarmOwnerSearchQueryById(), null, populateSqlParameters(buffHerd.Owner));

            if (farmOwnerRecordsCheck.Rows.Count == 0)
            {
                status = "Farm owner does not exists";
                return Conflict(status);
            }

            string farmOwner_update = $@"UPDATE [dbo].[tbl_FarmOwner] SET 
                                             [FirstName] = '" + registrationModel.Owner.FirstName + "'" +
                                            ",[LastName] = '" + registrationModel.Owner.LastName + "'" +
                                            ",[Address] = '" + registrationModel.Owner.Address + "'" +
                                            ",[TelephoneNumber] = '" + registrationModel.Owner.TelNo + "'" +
                                            ",[MobileNumber] = '" + registrationModel.Owner.MNo + "'" +
                                            ",[Email] = '" + registrationModel.Owner.Email + "'" +
                                            " WHERE id = '" + buffHerd.Owner + "'";
            string result = db.DB_WithParam(farmOwner_update);

            var farmOwner = convertDataRowToFarmOwnerEntity(farmOwnerRecordsCheck.Rows[0]);



            try
            {
                buffHerd = populateBuffHerd(buffHerd, registrationModel);

                buffHerd.buffaloType.Clear();
                buffHerd.feedingSystem.Clear();

                populateFeedingSystemAndBuffaloType(buffHerd, registrationModel);

                buffHerd.Owner = farmOwner.Id;
                buffHerd.DateUpdated = DateTime.Now;
                buffHerd.UpdatedBy = registrationModel.UpdatedBy;

                _context.Entry(buffHerd).State = EntityState.Modified;
                _context.SaveChanges();
                status = "Update Successful!";
                dbmet.InsertAuditTrail("Update Buffalo Herd" + " " + status, DateTime.Now.ToString("yyyy-MM-dd"), "Herd Module", buffHerd.UpdatedBy, "0");
                return Ok(status);
            }
            catch (Exception ex)
            {

                return Problem(ex.GetBaseException().ToString());
            }
        }

        // POST: BuffHerds/save
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<HBuffHerd>> save(BuffHerdRegistrationModel registrationModel)
        {

            try
            {


                string filePath = @"C:\data\herdsave.json"; // Replace with your desired file path



                dbmet.insertlgos(filePath, JsonSerializer.Serialize(registrationModel));
                DataTable buffHerdDuplicateCheck = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdDuplicateCheckSaveQuery(), null, populateSqlParameters(registrationModel.HerdName, registrationModel.HerdCode));

                if (buffHerdDuplicateCheck.Rows.Count > 0)
                {
                    status = "Herd already exists";
                    return Conflict(status);
                }

                var BuffHerdModel = buildBuffHerd(registrationModel);
                DataTable farmOwnerRecordsCheck = db.SelectDb_WithParamAndSorting(QueryBuilder.buildFarmOwnerSearchQueryByFirstNameAndLastName(), null, populateSqlParametersFarmer(registrationModel.Owner));

                if (farmOwnerRecordsCheck.Rows.Count == 0)
                {
                    // Create new Farm Owner Record
                    string user_insert = $@"INSERT INTO [dbo].[tbl_FarmOwner]
                                                ([FirstName]
                                                ,[LastName]
                                                ,[Address]
                                                ,[TelephoneNumber]
                                                ,[MobileNumber]
                                                ,[Email])
                                            VALUES
                                                ('" + registrationModel.Owner.FirstName + "'" +
                                                ",'" + registrationModel.Owner.LastName + "'," +
                                                "'" + registrationModel.Owner.Address + "'," +
                                                "'" + registrationModel.Owner.TelNo + "'," +
                                                "'" + registrationModel.Owner.MNo + "'," +
                                                "'" + registrationModel.Owner.Email + "')";
                    string test = db.DB_WithParam(user_insert);

                    DataTable farmOwnerRecord = db.SelectDb_WithParamAndSorting(QueryBuilder.buildFarmOwnerSearchQueryByFirstNameAndLastName(), null, populateSqlParametersFarmer(registrationModel.Owner));

                    var farmOwner = convertDataRowToFarmOwnerEntity(farmOwnerRecord.Rows[0]);
                    BuffHerdModel.Owner = farmOwner.Id;
                }
                else
                {
                    var farmOwner = convertDataRowToFarmOwnerEntity(farmOwnerRecordsCheck.Rows[0]);
                    BuffHerdModel.Owner = farmOwner.Id;
                }

                //populateFeedingSystemAndBuffaloType(BuffHerdModel, registrationModel);

                BuffHerdModel.CreatedBy = registrationModel.CreatedBy;
                BuffHerdModel.DateCreated = DateTime.Now;

                _context.HBuffHerds.Add(BuffHerdModel);
                await _context.SaveChangesAsync();



                string breed = $@"
                                SELECT [Join_BuffHerd_Id]
                                      ,[Buffalo_Type_Id]
                                      ,[Buff_Herd_id]
                                      ,[Feeding_System_Id]
                                  FROM [dbo].[tbl_Join_BuffHerd] where Buff_Herd_id ='" + BuffHerdModel.Id + "'";
                DataTable breed_tbl = db.SelectDb(breed).Tables[0];
                if (breed_tbl.Rows.Count != 0)
                {

                    string delete = $@"DELETE FROM [dbo].[tbl_Join_BuffHerd] where Buff_Herd_id ='" + BuffHerdModel.Id + "'";
                    db.DB_WithParam(delete);
                    foreach (string breedTypeCode in registrationModel.BreedTypeCodes)
                    {
                        string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                   ([Buffalo_Type_Id]
                                   ,[Buff_Herd_id]
                                   ,[Feeding_System_Id])
                             VALUES
                                  ('
                        " + breedTypeCode + "'" +
                                         ",'" + BuffHerdModel.Id + "'" +
                                        ",'0')";
                        string test = db.DB_WithParam(user_insert);
                    }
                    foreach (string feedingSystemCode in registrationModel.FeedingSystemCodes)
                    {
                        string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                     ([Feeding_System_Id]
                                       ,[Buff_Herd_id]
                                       ,[Buffalo_Type_Id])
                             VALUES
                                  ('" + feedingSystemCode + "'" +
                                         ",'" + BuffHerdModel.Id + "'" +
                                     ",'0')";
                        string test = db.DB_WithParam(user_insert);
                    }
                }
                else
                {
                    foreach (string breedTypeCode in registrationModel.BreedTypeCodes)
                    {
                        string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                     ([Buffalo_Type_Id]
                                       ,[Buff_Herd_id]
                                       ,[Feeding_System_Id])
                             VALUES
                                  ('" + breedTypeCode + "'" +
                                         ",'" + BuffHerdModel.Id + "'" +
                                        ",'0')";
                        string test = db.DB_WithParam(user_insert);
                    }
                    foreach (string feedingSystemCode in registrationModel.FeedingSystemCodes)
                    {
                        string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                    ([Feeding_System_Id]
                                       ,[Buff_Herd_id]
                                       ,[Buffalo_Type_Id])
                             VALUES
                                  ('" + feedingSystemCode + "'" +
                                         ",'" + BuffHerdModel.Id + "'" +
                                     ",'0')";
                        string test = db.DB_WithParam(user_insert);
                    }
                }
                status = "Herd successfully registered!";
                dbmet.InsertAuditTrail("Save Buffalo Herd" + " " + status, DateTime.Now.ToString("yyyy-MM-dd"), "Herd Module", registrationModel.CreatedBy, "0");

                return Ok(status);
            }
            catch (Exception ex)
            {
                dbmet.InsertAuditTrail("Save Buffalo Herd" + " " + ex.Message, DateTime.Now.ToString("yyyy-MM-dd"), "Herd Module", registrationModel.CreatedBy, "0");

                return Problem(ex.GetBaseException().ToString());
            }
        }
        private void populateFeedingSystemAndBuffaloType(HBuffHerd buffHerd, BuffHerdBaseModel baseModel)
        {
            var buffaloTypes = new List<HBuffaloType>();
            var feedingSystems = new List<HFeedingSystem>();
            int counter = 0;


            //DataTable buffaloType = db.SelectDb_WithParamAndSorting(QueryBuilder.buildBuffaloTypeSearchQueryByBreedTypeCode(), null, populateSqlParametersBuffaloType(breedTypeCode));
            //if (buffaloType.Rows.Count == 0)
            //{
            //    break;
            //}
            //var buffaloTypeRecord = convertDataRowToBuffaloType(buffaloType.Rows[0]);
            //_context.Attach(buffaloTypeRecord);
            //buffHerd.buffaloType.Add(buffaloTypeRecord);
            string breed = $@"
                                SELECT [Join_BuffHerd_Id]
                                      ,[Buffalo_Type_Id]
                                      ,[Buff_Herd_id]
                                      ,[Feeding_System_Id]
                                  FROM [dbo].[tbl_Join_BuffHerd] where Buff_Herd_id ='" + buffHerd.Id + "'";
            DataTable breed_tbl = db.SelectDb(breed).Tables[0];
            if (breed_tbl.Rows.Count != 0)
            {

                string delete = $@"DELETE FROM [dbo].[tbl_Join_BuffHerd] where Buff_Herd_id ='" + buffHerd.Id + "'";
                db.DB_WithParam(delete);
                foreach (string breedTypeCode in baseModel.BreedTypeCodes)
                {
                    string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                   ([Buffalo_Type_Id]
                                   ,[Buff_Herd_id]
                                   ,[Feeding_System_Id])
                             VALUES
                                  ('" + breedTypeCode + "'" +
                                     ",'" + buffHerd.Id + "'" +
                                    ",'0')";
                    string test = db.DB_WithParam(user_insert);
                }
                foreach (string feedingSystemCode in baseModel.FeedingSystemCodes)
                {
                    string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                     ([Feeding_System_Id]
                                       ,[Buff_Herd_id]
                                       ,[Buffalo_Type_Id])
                             VALUES
                                  ('" + feedingSystemCode + "'" +
                                     ",'" + buffHerd.Id + "'" +
                                 ",'0')";
                    string test = db.DB_WithParam(user_insert);
                }
            }
            else
            {
                foreach (string breedTypeCode in baseModel.BreedTypeCodes)
                {
                    string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                     ([Buffalo_Type_Id]
                                       ,[Buff_Herd_id]
                                       ,[Feeding_System_Id])
                             VALUES
                                  ('" + breedTypeCode + "'" +
                                     ",'" + buffHerd.Id + "'" +
                                    ",'0')";
                    string test = db.DB_WithParam(user_insert);
                }
                foreach (string feedingSystemCode in baseModel.FeedingSystemCodes)
                {
                    string user_insert = $@"INSERT INTO [dbo].[tbl_Join_BuffHerd]
                                    ([Feeding_System_Id]
                                       ,[Buff_Herd_id]
                                       ,[Buffalo_Type_Id])
                             VALUES
                                  ('" + feedingSystemCode + "'" +
                                     ",'" + buffHerd.Id + "'" +
                                 ",'0')";
                    string test = db.DB_WithParam(user_insert);
                }
            }




            //foreach (string feedingSystemCode in baseModel.FeedingSystemCodes)
            //{
            //    DataTable feedingSystem = db.SelectDb_WithParamAndSorting(QueryBuilder.buildFeedingSystemSearchByFeedingSystemCode(), null, populateSqlParametersFeedingSystem(feedingSystemCode));
            //    if (feedingSystem.Rows.Count == 0)
            //    {
            //        break;
            //    }
            //    var feedingSystemRecord = convertDataRowToFeedingSystem(feedingSystem.Rows[0]);
            //    _context.Attach(feedingSystemRecord);
            //    buffHerd.feedingSystem.Add(feedingSystemRecord);
            //}


        }
        private TblFarmOwner convertDataRowToFarmOwnerEntity(DataRow dataRow)
        {
            var farmOwner = DataRowToObject.ToObject<TblFarmOwner>(dataRow);

            return farmOwner;
        }

        // DELETE: BuffHerds/delete/5
        [HttpPost]
        public async Task<IActionResult> delete(DeletionModel deletionModel)
        {
            if (_context.HBuffHerds == null)
            {
                return NotFound();
            }
            var hBuffHerd = await _context.HBuffHerds.FindAsync(deletionModel.id);
            if (hBuffHerd == null || hBuffHerd.DeleteFlag)
            {
                return Conflict("No records matched!");
            }

            try
            {
                hBuffHerd.DeleteFlag = true;
                hBuffHerd.DateDeleted = DateTime.Now;
                hBuffHerd.DeletedBy = deletionModel.deletedBy;
                hBuffHerd.DateRestored = null;
                hBuffHerd.RestoredBy = "";
                _context.Entry(hBuffHerd).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok("Deletion Successful!");
            }
            catch (Exception ex)
            {

                return Problem(ex.GetBaseException().ToString());
            }
        }

        // POST: BuffHerds/restore/
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> restore(RestorationModel restorationModel)
        {
            DataTable dt = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdSelectForRestoreQuery(), null, populateSqlParameters(restorationModel.id));

            if (dt.Rows.Count == 0)
            {
                return Conflict("No deleted records matched!");
            }

            var herdModel = convertDataRowToHerdModel(dt.Rows[0]);

            DataTable buffHerdDuplicateCheck = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdDuplicateCheckSaveQuery(), null, populateSqlParameters(herdModel.HerdName, herdModel.HerdCode));

            if (buffHerdDuplicateCheck.Rows.Count > 0)
            {
                return Conflict("Entity already exists!!");
            }

            try
            {
                herdModel.DeleteFlag = !herdModel.DeleteFlag;
                herdModel.DateDeleted = null;
                herdModel.DeletedBy = "";
                herdModel.DateRestored = DateTime.Now;
                herdModel.RestoredBy = restorationModel.restoredBy;

                _context.Entry(herdModel).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok("Restoration Successful!");
            }
            catch (Exception ex)
            {

                return Problem(ex.GetBaseException().ToString());
            }
        }

        private List<HerdPagedModel> buildHerdPagedModel(BuffHerdSearchFilterModel searchFilter, List<HBuffHerd> buffHerdList)
        {

            int pagesize = searchFilter.pageSize == 0 ? 10 : searchFilter.pageSize;
            int page = searchFilter.page == 0 ? 1 : searchFilter.page;
            var items = (dynamic)null;

            int totalItems = buffHerdList.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pagesize);
            items = buffHerdList.Skip((page - 1) * pagesize).Take(pagesize).ToList();

            //var herdModels = convertDataRowListToHerdModelList(items);
            List<BuffHerdListResponseModel> buffHerdBaseModels = convertBuffHerdToResponseModelList(buffHerdList);

            var result = new List<HerdPagedModel>();
            var item = new HerdPagedModel();

            int pages = searchFilter.page == 0 ? 1 : searchFilter.page;
            item.CurrentPage = searchFilter.page == 0 ? "1" : searchFilter.page.ToString();
            int page_prev = pages - 1;

            double t_records = Math.Ceiling(Convert.ToDouble(totalItems) / Convert.ToDouble(pagesize));
            int page_next = searchFilter.page >= t_records ? 0 : pages + 1;
            item.NextPage = items.Count % pagesize >= 0 ? page_next.ToString() : "0";
            item.PrevPage = pages == 1 ? "0" : page_prev.ToString();
            item.TotalPage = t_records.ToString();
            item.PageSize = pagesize.ToString();
            item.TotalRecord = totalItems.ToString();
            item.items = buffHerdBaseModels;
            result.Add(item);

            return result;
        }
        private List<HBuffHerd> convertDataRowListToHerdModelList(List<DataRow> dataRowList)
        {
            var herdModelList = new List<HBuffHerd>();

            foreach (DataRow dataRow in dataRowList)
            {
                var herdModel = DataRowToObject.ToObject<HBuffHerd>(dataRow);
                herdModelList.Add(herdModel);
            }

            return herdModelList;
        }
        private HBuffHerd convertDataRowToHerdModel(DataRow dataRow)
        {
            return DataRowToObject.ToObject<HBuffHerd>(dataRow);
        }

        private HHerdClassification convertDataRowToHerdClassification(DataRow dataRow)
        {
            return DataRowToObject.ToObject<HHerdClassification>(dataRow);
        }

        private HBuffaloType convertDataRowToBuffaloType(DataRow dataRow)
        {
            return DataRowToObject.ToObject<HBuffaloType>(dataRow);
        }

        private HFeedingSystem convertDataRowToFeedingSystem(DataRow dataRow)
        {
            return DataRowToObject.ToObject<HFeedingSystem>(dataRow);
        }



        private HBuffHerd populateBuffHerd(HBuffHerd buffHerd, BuffHerdUpdateModel updateModel)
        {

            if (updateModel.HerdName != null && updateModel.HerdName != "")
            {
                buffHerd.HerdName = updateModel.HerdName;
            }
            if (updateModel.HerdCode != null && updateModel.HerdCode != "")
            {
                buffHerd.HerdCode = updateModel.HerdCode;
            }
            if (updateModel.FarmAffilCode != null && updateModel.FarmAffilCode != "")
            {
                buffHerd.FarmAffilCode = updateModel.FarmAffilCode;
            }
            if (updateModel.HerdClassDesc != null && updateModel.HerdClassDesc != "")
            {
                buffHerd.HerdClassDesc = updateModel.HerdClassDesc;
            }
            if (updateModel.FarmManager != null && updateModel.FarmManager != "")
            {
                buffHerd.FarmManager = updateModel.FarmManager;
            }
            if (updateModel.FarmAddress != null && updateModel.FarmAddress != "")
            {
                buffHerd.FarmAddress = updateModel.FarmAddress;
            }
            if (updateModel.OrganizationName != null && updateModel.OrganizationName != "")
            {
                buffHerd.OrganizationName = updateModel.OrganizationName;
            }
            if (updateModel.Photo != null && updateModel.Photo != "")
            {
                buffHerd.Photo = updateModel.Photo;
            }
            return buffHerd;
        }
        private HBuffHerd buildBuffHerd(BuffHerdBaseModel registrationModel)
        {
            var BuffHerdModel = new HBuffHerd()
            {
                HerdName = registrationModel.HerdName,
                HerdCode = registrationModel.HerdCode,
                HerdSize = registrationModel.HerdSize,
                FarmAffilCode = registrationModel.FarmAffilCode,
                HerdClassDesc = registrationModel.HerdClassDesc,
                FarmManager = registrationModel.FarmManager,
                FarmAddress = registrationModel.FarmAddress,
                OrganizationName = registrationModel.OrganizationName,
                Center = registrationModel.Center,
                Photo = registrationModel.Photo
            };

            return BuffHerdModel;
        }
        private List<BuffHerdListResponseModel> convertBuffHerdToResponseModelList(List<HBuffHerd> buffHerdList)
        {
            var buffHerdResponseModels = new List<BuffHerdListResponseModel>();
            foreach (HBuffHerd buffHerd in buffHerdList)
            {
                string tbl = $@"SELECT  Herd_Class_Desc FROM H_Herd_Classification where Herd_Class_Code='" + buffHerd.HerdClassDesc + "'";
                DataTable tbl_hc = db.SelectDb(tbl).Tables[0];

                string cow_lvl = $@"SELECT  count(*) as cowlevel FROM A_Buff_Animal where Herd_Code ='" + buffHerd.HerdCode + "' ";
                DataTable cow_lvl_tbl = db.SelectDb(cow_lvl).Tables[0];
                string cow_res = cow_lvl_tbl.Rows.Count == 0 ? "0" : cow_lvl_tbl.Rows[0]["cowlevel"].ToString();
                if (tbl_hc.Rows.Count != 0)
                {

                    var buffHerdResponseModel = new BuffHerdListResponseModel()
                    {
                        Id = buffHerd.Id.ToString(),
                        HerdName = buffHerd.HerdName,
                        HerdClassification = tbl_hc.Rows[0]["Herd_Class_Desc"].ToString(),
                        CowLevel = cow_res,
                        FarmManager = buffHerd.FarmManager,
                        HerdCode = buffHerd.HerdCode,
                        Photo = buffHerd.Photo,
                        DateOfApplication = buffHerd.DateCreated.ToString("yyyy-MM-dd")
                    };
                    buffHerdResponseModels.Add(buffHerdResponseModel);

                }
            }

            return buffHerdResponseModels;
        }

        private BuffHerdListResponseModel convertBuffHerdToResponseModel(HBuffHerd buffHerd)
        {
            var buffHerdResponseModel = new BuffHerdListResponseModel()
            {
                HerdName = buffHerd.HerdName,
                HerdClassification = buffHerd.HerdClassDesc,
                CowLevel = buffHerd.HerdSize.ToString(),
                FarmManager = buffHerd.FarmManager,
                HerdCode = buffHerd.HerdCode,

                Photo = buffHerd.Photo,
                DateOfApplication = buffHerd.DateCreated.ToString("yyyy-MM-dd")
            };
            return buffHerdResponseModel;
        }

        private Owner populateOwner(int ownerId)
        {
            DataTable queryResult = db.SelectDb_WithParamAndSorting(QueryBuilder.buildFarmOwnerSearchQueryById(), null, populateSqlParameters(ownerId));

            if (queryResult.Rows.Count == 0)
            {
                return new Owner()
                {
                    Id = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    Address = string.Empty,
                    Email = string.Empty,
                    MNo = string.Empty,
                    TelNo = string.Empty
                };
            }
            var farmOwnerEntity = convertDataRowToFarmOwnerEntity(queryResult.Rows[0]);

            var owner = new Owner()
            {
                Id = farmOwnerEntity.Id.ToString(),
                FirstName = farmOwnerEntity.FirstName,
                LastName = farmOwnerEntity.LastName,
                Address = farmOwnerEntity.Address,
                Email = farmOwnerEntity.Email,
                MNo = farmOwnerEntity.MobileNumber,
                TelNo = farmOwnerEntity.TelephoneNumber
            };

            return owner;
        }

        private HHerdClassification populateHerdClassification(string herdClassDesc)
        {
            DataTable queryResult = db.SelectDb_WithParamAndSorting(QueryBuilder.buildHerdClassificationSearchQueryByHerdClassDesc2(), null, populateSqlParametersHerdClassDesc(herdClassDesc));
            if (queryResult.Rows.Count == 0)
            {
                return new HHerdClassification()
                {
                    HerdClassCode = string.Empty,
                    HerdClassDesc = string.Empty,
                    Status = new int(),
                    LevelFrom = 0,
                    LevelTo = 0,
                };
            }
            var herdClassificationEntity = convertDataRowToHerdClassification(queryResult.Rows[0]);

            var herdClassification = new HHerdClassification()
            {
                HerdClassCode = herdClassificationEntity.HerdClassCode,
                HerdClassDesc = herdClassificationEntity.HerdClassDesc,
                Status = herdClassificationEntity.Status,
                LevelFrom = herdClassificationEntity.LevelFrom,
                LevelTo = herdClassificationEntity.LevelTo,
            };


            return herdClassification;
        }

        private SqlParameter[] populateSqlParameters(BuffHerdSearchFilterModel searchFilter)
        {

            var sqlParameters = new List<SqlParameter>();

            if (searchFilter.searchValue != null && searchFilter.searchValue != "")
            {
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "SearchParam",
                    Value = searchFilter.searchValue ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.VarChar,
                });
            }

            if (searchFilter.filterBy != null)
            {
                if (searchFilter.filterBy.BreedTypeCode != null && searchFilter.filterBy.BreedTypeCode != "")
                {
                    sqlParameters.Add(new SqlParameter
                    {
                        ParameterName = "BreedTypeCode",
                        Value = searchFilter.filterBy.BreedTypeCode ?? Convert.DBNull,
                        SqlDbType = System.Data.SqlDbType.VarChar,
                    });
                }

                if (searchFilter.filterBy.HerdClassDesc != null && searchFilter.filterBy.HerdClassDesc != "")
                {
                    sqlParameters.Add(new SqlParameter
                    {
                        ParameterName = "HerdClassDesc",
                        Value = searchFilter.filterBy.HerdClassDesc ?? Convert.DBNull,
                        SqlDbType = System.Data.SqlDbType.VarChar,
                    });
                }

                if (searchFilter.filterBy.feedingSystemCode != null && searchFilter.filterBy.feedingSystemCode != "")
                {
                    sqlParameters.Add(new SqlParameter
                    {
                        ParameterName = "FeedingSystemCode",
                        Value = searchFilter.filterBy.feedingSystemCode ?? Convert.DBNull,
                        SqlDbType = System.Data.SqlDbType.VarChar,
                    });
                }
            }

            if (searchFilter.dateFrom != null && searchFilter.dateFrom != "")
            {
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "DateFrom",
                    Value = searchFilter.dateFrom == "" ? Convert.DBNull : searchFilter.dateFrom,
                    SqlDbType = System.Data.SqlDbType.VarChar,
                });
            }

            if (searchFilter.dateTo != "")
            {
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "DateTo",
                    Value = searchFilter.dateTo == "" ? Convert.DBNull : searchFilter.dateTo,
                    SqlDbType = System.Data.SqlDbType.VarChar,
                });
            }

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParameters(String herdCode)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "HerdCode",
                Value = herdCode ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParameters(String herdCode, String herdName)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "HerdCode",
                Value = herdCode ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "HerdName",
                Value = herdName ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParametersHerdClassDesc(String herdClassDesc)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "HerdClassDesc",
                Value = herdClassDesc ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParameters(int id)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Id",
                Value = id,
                SqlDbType = System.Data.SqlDbType.Int,
            });

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParameters(int id, BuffHerdUpdateModel registrationModel)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Id",
                Value = id,
                SqlDbType = System.Data.SqlDbType.Int,
            });


            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "HerdName",
                Value = registrationModel.HerdName ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });


            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "HerdCode",
                Value = registrationModel.HerdCode ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParametersFarmer(Owner owner)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "FirstName",
                Value = owner.FirstName ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });


            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "LastName",
                Value = owner.LastName ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }
        private SqlParameter[] populateSqlParametersBuffaloType(String breedTypeCode)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "breedTypeCode",
                Value = breedTypeCode ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }
        private SqlParameter[] populateSqlParametersFeedingSystem(String feedingSystemCode)
        {

            var sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "FeedCode",
                Value = feedingSystemCode ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }
        private void sanitizeInput(BuffHerdSearchFilterModel searchFilter)
        {
            searchFilter.searchValue = StringSanitizer.sanitizeString(searchFilter.searchValue);
            searchFilter.dateFrom = StringSanitizer.sanitizeString(searchFilter.dateFrom);
            searchFilter.dateTo = StringSanitizer.sanitizeString(searchFilter.dateTo);
            searchFilter.filterBy.feedingSystemCode = StringSanitizer.sanitizeString(searchFilter.filterBy.feedingSystemCode);
            searchFilter.filterBy.BreedTypeCode = StringSanitizer.sanitizeString(searchFilter.filterBy.BreedTypeCode);
            searchFilter.filterBy.HerdClassDesc = StringSanitizer.sanitizeString(searchFilter.filterBy.HerdClassDesc);
            searchFilter.sortBy.Field = StringSanitizer.sanitizeString(searchFilter.sortBy.Field);
            searchFilter.sortBy.Sort = StringSanitizer.sanitizeString(searchFilter.sortBy.Sort);
        }

        private BuffHerdViewResponseModel populateViewResponseModel(HBuffHerd buffHerd)
        {
            var herdClassification = populateHerdClassification(buffHerd.HerdClassDesc);
            string tbl = $@"SELECT  Herd_Class_Desc,Herd_Class_Code FROM H_Herd_Classification where Herd_Class_Code='" + buffHerd.HerdClassDesc + "'";
            DataTable tbl_hc = db.SelectDb(tbl).Tables[0];

            string cow_lvl = $@"SELECT  count(*) as cowlevel FROM A_Buff_Animal where Herd_Code ='" + buffHerd.HerdCode + "' ";
            DataTable cow_lvl_tbl = db.SelectDb(cow_lvl).Tables[0];
            string cow_res = cow_lvl_tbl.Rows.Count == 0 ? "0" : cow_lvl_tbl.Rows[0]["cowlevel"].ToString();
            var bloodCompRecord = _context.HHerdClassifications.Where(bloodComp => bloodComp.LevelFrom <= int.Parse(cow_res) && bloodComp.LevelTo >= int.Parse(cow_res)).FirstOrDefault();
            string classcode = cow_res == "0" ? tbl_hc.Rows[0]["Herd_Class_Code"].ToString() : bloodCompRecord.HerdClassCode;
            string classdesc = cow_res == "0" ? tbl_hc.Rows[0]["Herd_Class_Desc"].ToString() : bloodCompRecord.HerdClassDesc;
            var viewResponseModel = new BuffHerdViewResponseModel()
            {
                id = buffHerd.Id,
                HerdName = buffHerd.HerdName,
                HerdClassDesc = classdesc,
                HerdClassCode = classcode,
                HerdSize = int.Parse(cow_res),
                FarmManager = buffHerd.FarmManager,
                HerdCode = buffHerd.HerdCode,
                FarmAffilCode = buffHerd.FarmAffilCode,
                FarmAddress = buffHerd.FarmAddress,
                Owner = populateOwner(buffHerd.Owner),
                Status = buffHerd.Status,
                OrganizationName = buffHerd.OrganizationName,
                Center = buffHerd.Center,
                Photo = buffHerd.Photo,
                DateCreated = buffHerd.DateCreated,
                CreatedBy = buffHerd.CreatedBy,
                DeleteFlag = buffHerd.DeleteFlag,
                DateUpdated = buffHerd.DateUpdated,
                UpdatedBy = buffHerd.UpdatedBy,
                DateDeleted = buffHerd.DateDeleted,
                DeletedBy = buffHerd.DeletedBy,
                DateRestored = buffHerd.DateRestored,
                RestoredBy = buffHerd.RestoredBy
            };

            var buffaloTypeList = new List<string>();
            var feedingSystemList = new List<string>();
            string tbl1 = $@"
                        SELECT [Buffalo_Type_Id]
       ,[Buff_Herd_id],
	   Feeding_System_Id
   FROM [dbo].[tbl_Join_BuffHerd] WHERE Buff_Herd_id =  '" + buffHerd.Id + "' GROUP BY [Buffalo_Type_Id] ,[Buff_Herd_id],Feeding_System_Id";
            DataTable tbl_hc1 = db.SelectDb(tbl1).Tables[0];
            foreach (DataRow dr in tbl_hc1.Rows)
            {
                string feeding = $@"SELECT [id]
                                  ,[FeedingSystemCode]
                                  ,[FeedingSystemDesc]
                              FROM [dbo].[H_Feeding_System] WHERE id='" + dr["Feeding_System_Id"].ToString() + "' AND FeedingSystemCode <> 0 ";
                DataTable feeding_tbl = db.SelectDb(feeding).Tables[0];
                foreach (DataRow fd in feeding_tbl.Rows)
                {
                    feedingSystemList.Add(fd["id"].ToString());
                }
                string breed = $@"SELECT [id]
                              ,[Breed_Code]
                              ,[Breed_Desc]
                          FROM [dbo].[A_Breed] WHERE id ='" + dr["Buffalo_Type_Id"].ToString() + "'";
                DataTable breed_tbl = db.SelectDb(breed).Tables[0];
                foreach (DataRow brd in breed_tbl.Rows)
                {
                    buffaloTypeList.Add(brd["id"].ToString());
                }

            }

            //foreach (HBuffaloType buffaloType in buffHerd.buffaloType)
            //{
            //    buffaloTypeList.Add(buffaloType.BreedTypeCode);
            //}

            //foreach (HFeedingSystem feedingSystem in buffHerd.feedingSystem)
            //{
            //    feedingSystemList.Add(feedingSystem.FeedingSystemCode);
            //}




            viewResponseModel.BreedTypeCode.AddRange(buffaloTypeList);
            viewResponseModel.FeedingSystemCode.AddRange(feedingSystemList);
            return viewResponseModel;
        }

        private void validateDate(BuffHerdSearchFilterModel searchFilter)
        {

            if (!searchFilter.dateFrom.IsNullOrEmpty())
            {
                if (!DateTime.TryParse(searchFilter.dateFrom, out DateTime dateTimeFrom))
                {
                    throw new System.FormatException("Date From is not a valid Date!");
                }
            }

            if (!searchFilter.dateTo.IsNullOrEmpty())
            {
                if (!DateTime.TryParse(searchFilter.dateTo, out DateTime dateTimeTo))
                {
                    throw new System.FormatException("Date To is not a valid Date!");
                }
            }
        }

    }

}
