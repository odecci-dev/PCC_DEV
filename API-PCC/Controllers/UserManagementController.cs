using API_PCC.ApplicationModels;
using API_PCC.ApplicationModels.Common;
using API_PCC.Data;
using API_PCC.EntityModels;
using API_PCC.Manager;
using API_PCC.Models;
using API_PCC.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using static API_PCC.Controllers.UserController;
//using static API_PCC.Manager.DBMethods;

namespace API_PCC.Controllers
{
    [Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly PCC_DEVContext _context;
        DbManager db = new DbManager();
        DBMethods dbmet = new DBMethods();
        string Stats = "";
        string Mess = "";
        string JWT = "";
        public UserManagementController(PCC_DEVContext context)
        {
            _context = context;
        }
        public class Module_Model
        {
            public string? ModuleId { get; set; }
            public string? ModuleName { get; set; }
            public string? ParentModule { get; set; }
            public string? DateCreated { get; set; }
            public List<ActionModel> Actions { get; set; }
        }
        public class SaveModule_Model
        {
            public string? ModuleId { get; set; }
            public List<SaveActionModel> Actions { get; set; }
        }
        public class UserTypeAction_Model
        {
            public string? UserTypeId { get; set; }
            public string? UserType { get; set; }
            public List<Module_Model> Module { get; set; }
        }
        public class SaveUserTypeAction_Model
        {
            public string? UserTypeId { get; set; }
            public List<SaveModule_Model> Module { get; set; }
        }
        public class SaveActionModel
        {
            public string? ActionId { get; set; }
        }
        public class ActionModel
        {
            public string? ActionId { get; set; }
            public string? Actions { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<UserTypeAction_Model>>> UserTypeModuleActionList(BirthTypesSearchFilterModel searchFilter)
        {
            try
            {
                string sql = $@"SELECT [Id]
                      ,Name as UserType
                  FROM [dbo].[tbl_UserTypeModel]";
                var result = new List<UserTypeAction_Model>();
                DataTable table = db.SelectDb(sql).Tables[0];

                foreach (DataRow dr in table.Rows)
                {
                    var item = new UserTypeAction_Model();
                    item.UserTypeId = dr["Id"].ToString();
                    item.UserType = dr["UserType"].ToString();
                    string sql_usertype = $@"SELECT        User_ModuleTable.Id as ModuleId,User_UserTypeAccessTable.UserTypeId, User_ModuleTable.Module, User_ModuleTable.ParentModule,User_UserTypeAccessTable.DateCreated
                                        FROM            User_UserTypeAccessTable INNER JOIN
                                                                 User_ModuleTable ON User_UserTypeAccessTable.Module = User_ModuleTable.Id  where User_UserTypeAccessTable.UserTypeId ='" + dr["Id"].ToString() + "' ";
                    DataTable tbl_usertype = db.SelectDb(sql_usertype).Tables[0];
                    var usertype_item = new List<Module_Model>();
                    foreach (DataRow drw in tbl_usertype.Rows)
                    {
                        var item1 = new Module_Model();
                        item1.ModuleId = drw["ModuleId"].ToString();
                        item1.ModuleName = drw["Module"].ToString();
                        item1.ParentModule = drw["ParentModule"].ToString();
                        item1.DateCreated = drw["DateCreated"].ToString();

                        string sql_actions = $@"SELECT  [Id]
                                      ,[ActionName]
                                      ,[DateCreated]
                                  FROM [PCC_DEV].[dbo].[User_ActionTable] where Module ='" + drw["ModuleId"].ToString() + "'";
                        DataTable action_tbl = db.SelectDb(sql_actions).Tables[0];
                        var action_item = new List<ActionModel>();
                        foreach (DataRow dra in action_tbl.Rows)
                        {
                            var items = new ActionModel();
                            items.ActionId = dra["Id"].ToString();
                            items.Actions = dra["ActionName"].ToString();
                            action_item.Add(items);

                        }
                        item1.Actions = action_item;
                        usertype_item.Add(item1);

                    }
                    item.Module = usertype_item;

                    result.Add(item);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(ex.GetBaseException().ToString());
            }
        }
        [HttpPost]
        public async Task<ActionResult<IEnumerable<ABirthType>>> ModuleActionList(BirthTypesSearchFilterModel searchFilter)
        {
            try
            {
                string sql = $@"SELECT [Id]
                      ,[Module]
                      ,[ParentModule]
                      ,[DateCreated]
                  FROM [dbo].[User_ModuleTable]";
                var result = new List<Module_Model>();
                DataTable table = db.SelectDb(sql).Tables[0];

                foreach (DataRow dr in table.Rows)
                {
                    var item = new Module_Model();
                    item.ModuleId = dr["Id"].ToString();
                    item.ModuleName = dr["Module"].ToString();
                    item.ParentModule = dr["ParentModule"].ToString();
                    item.DateCreated = dr["DateCreated"].ToString();
                    string sql_actions = $@"SELECT  [Id]
                                      ,[ActionName]
                                      ,[DateCreated]
                                  FROM [PCC_DEV].[dbo].[User_ActionTable] where Module ='" + dr["Id"].ToString() + "'";
                    DataTable action_tbl = db.SelectDb(sql_actions).Tables[0];
                    var action_item = new List<ActionModel>();
                    foreach (DataRow drw in action_tbl.Rows)
                    {
                        var items = new ActionModel();
                        items.ActionId = drw["Id"].ToString();
                        items.Actions = drw["ActionName"].ToString();
                        action_item.Add(items);

                    }
                    item.Actions = action_item;
                    result.Add(item);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Problem(ex.GetBaseException().ToString());
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveModuleAcess(SaveUserTypeAction_Model data)
        {
            try
            {
                string sql = $@"SELECT [Id]
                  FROM [dbo].[tbl_UserTypeModel] where Id='" + data.UserTypeId + "'";
                var result = new List<UserTypeAction_Model>();
                DataTable table = db.SelectDb(sql).Tables[0];
                if (table.Rows.Count != 0)
                {
                    string sql1 = $@"SELECT *
                    FROM [dbo].[User_UserTypeAccessTable] where Id='" + data.UserTypeId + "'";
                    var result1 = new List<UserTypeAction_Model>();
                    DataTable table1 = db.SelectDb(sql1).Tables[0];
                    //string user_insert = $@"INSERT INTO [dbo].[User_UserTypeAccessTable]
                    //                ([Feeding_System_Id]
                    //                   ,[Buff_Herd_id]
                    //                   ,[Buffalo_Type_Id])
                    //         VALUES
                    //              ('" + feedingSystemCode + "'" +
                    //          ",'" + buffHerd.Id + "'" +
                    //      ",'0')";
                    //string test = db.DB_WithParam(user_insert);
                }

                return Ok("");
            }
            catch (Exception ex)
            {
                return Problem(ex.GetBaseException().ToString());
            }
        }


        private IQueryable<TblUsersModel> buildUserManagementSearchQuery(Dictionary<string, object> filter)
        {
            IQueryable<TblUsersModel> query = _context.TblUsersModels;

            query = query
                .Include(user => user.userAccessModels)
                .ThenInclude(userAccessModel => userAccessModel.userAccess)
                .Where(user => !user.DeleteFlag);


            // assuming that you return all records when nothing is specified in the filter

            if (filter.ContainsKey("searchParam"))
            {
                var searchParam = filter["searchParam"].ToString();
                query = query.Where(user =>
                               user.Fname.Contains(searchParam) ||
                               user.Lname.Contains(searchParam) ||
                               user.Mname.Contains(searchParam) ||
                               user.Email.Contains(searchParam));
            }

            if (filter.ContainsKey("forApproval") && Convert.ToBoolean(filter["forApproval"]))
            {
                query = query.Where(user => user.Status.Equals(3));
            }

            if (filter.ContainsKey("username"))
            {
                var username = filter["username"].ToString();
                query = query.Where(user => user.Username.Equals(username));
            }


            if (filter.ContainsKey("Id"))
            {
                var id = filter["Id"];
                query = query.Where(user => user.Id.Equals(id));
            }

            query = query.OrderByDescending(e => e.Id);

            return query;
        }

        [HttpPost]
        public async Task<ActionResult<TblUsersModel>> msbuff_Registration(RegistrationModel userTbl)
        {
            string filepath = "";
            var user_list = _context.TblUsersModels.AsEnumerable().Where(a => a.Username.Equals(userTbl.Username, StringComparison.Ordinal)).ToList();
            if (user_list.Count == 0)
            {
                var email_count = _context.TblUsersModels.Where(a => a.Email == userTbl.Email).ToList();
                if (email_count.Count != 0)
                {
                    Stats = "Error";
                    Mess = "Email Already Used!";
                    JWT = "";
                }
                else
                {
                    StringBuilder str_build = new StringBuilder();
                    Random random = new Random();
                    int length = 8;
                    char letter;

                    for (int i = 0; i < length; i++)
                    {
                        double flt = random.NextDouble();
                        int shift = Convert.ToInt32(Math.Floor(25 * flt));
                        letter = Convert.ToChar(shift + 2);
                        str_build.Append(letter);
                    }

                    var token = Cryptography.Encrypt(str_build.ToString());
                    string strtokenresult = token;
                    string[] charsToRemove = new string[] { "/", ",", ".", ";", "'", "=", "+" };
                    foreach (var c in charsToRemove)
                    {
                        strtokenresult = strtokenresult.Replace(c, string.Empty);
                    }
                    if (userTbl.FilePath == null)
                    {
                        filepath = "";
                    }
                    else
                    {
                        filepath = userTbl.FilePath.Replace(" ", "%20");
                    }
                    string fullname = userTbl.Fname + ", " + userTbl.Mname + ", " + userTbl.Lname;
                    string user_insert = $@"INSERT INTO [dbo].[tbl_UsersModel]
                                           ([Username]
                                           ,[Password]
                                           ,[Fullname]
                                           ,[Fname]
                                           ,[Lname]
                                           ,[Mname]
                                           ,[Email]
                                           ,[Gender]
                                           ,[EmployeeID]
                                           ,[JWToken]
                                           ,[FilePath]
                                           ,[Active]
                                           ,[Cno]
                                           ,[Address]
                                           ,[Status]
                                           ,[Date_Created]
                                           ,[CenterId]
                                           ,[AgreementStatus]
                                           ,[UserType]
                                           ,[Delete_Flag])
                                     VALUES
                                           ('" + userTbl.Username + "'" +
                                            ",'" + Cryptography.Encrypt(userTbl.Password) + "'," +
                                           "'" + fullname + "'," +
                                           "'" + userTbl.Fname + "'," +
                                           "'" + userTbl.Lname + "'," +
                                           "'" + userTbl.Mname + "'," +
                                           "'" + userTbl.Email + "'," +
                                           "'" + userTbl.Gender + "'," +
                                           "'" + userTbl.EmployeeId + "'," +
                                           "'" + string.Concat(strtokenresult.TakeLast(15)) + "'," +
                                           "'" + filepath + "'," +
                                           "'1'," +
                                           "'" + userTbl.Cno + "'," +
                                           "'" + userTbl.Address + "'," +
                                           "'5'," +
                                           "'" + DateTime.Now.ToString("yyyy-MM-dd") + "'," +
                                           "'" + userTbl.CenterId + "'," +
                                           "'" + userTbl.AgreementStatus + "'," +
                                           "'" + userTbl.UserType + "'," +
                                           "'0')";
                    db.DB_WithParam(user_insert);




                    Stats = "Ok";
                    Mess = "Successfully Registered!";
                    JWT = string.Concat(strtokenresult.TakeLast(15));
                }
            }
            else
            {
                Stats = "Error";
                Mess = "Username Already Exist!";
                JWT = "";
            }
            StatusReturns result = new StatusReturns
            {
                Status = Stats,
                Message = Mess,
                JwtToken = JWT
            };
            string sqls = $@"select Username from tbl_UsersModel where Username ='" + userTbl.Username + "'";
            DataTable table = db.SelectDb(sqls).Tables[0];
            dbmet.InsertAuditTrail("Missbuff Registration " + Stats + " " + Mess, DateTime.Now.ToString("yyyy-MM-dd"), "User Management  Module", userTbl.Username, "0");
            return Ok(result);
        }
        public class ChangePassword
        {
            public int Id { get; set; }
            public string Password { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> msbuff_Changepassword(ChangePassword data)
        {


            try
            {
                string sql = $@"select  * from tbl_UsersModel  where   Id='" + data.Id + "' and Delete_Flag = 'False'";
                DataTable dt = db.SelectDb(sql).Tables[0];
                if (dt.Rows.Count == 0)
                {

                    dbmet.InsertAuditTrail("Missbuff Registration Change password No Record Found!", DateTime.Now.ToString("yyyy-MM-dd"), "User Management  Module", dt.Rows[0]["Username"].ToString(), "0");

                    return BadRequest("No Record found");


                }
                else
                {
                    string query = $@"Update  tbl_UsersModel set Password = '" + Cryptography.Encrypt(data.Password) + "' where  Id='" + data.Id + "'";
                    db.DB_WithParam(query);

                    dbmet.InsertAuditTrail("Missbuff Registration Password Successfully Changed", DateTime.Now.ToString("yyyy-MM-dd"), "User Management Module", dt.Rows[0]["Username"].ToString(), "0");

                    return Ok("Password Successfully Changed");
                }

            }

            catch (Exception ex)
            {
                return BadRequest("Error!");
            }

        }
        //[HttpPost]
        //public async Task<ActionResult<IEnumerable<UserPagedModel>>> UserForApprovalList(CommonSearchFilterModel searchFilter)
        //{

        //    try
        //    {
        //        var filter = new Dictionary<string, object>();
        //        filter.Add("forApproval", true);
        //        List<TblUsersModel> userList = await buildUserManagementSearchQuery(filter).ToListAsync();
        //        var result = buildUserPagedModel(searchFilter, userList);
        //        return Ok(result);
        //    }

        //    catch (Exception ex)
        //    {

        //        return Problem(ex.GetBaseException().ToString());
        //    }
        //}
        [HttpPost]
        public async Task<ActionResult<IEnumerable<TblUsersModel>>> UserForApprovalList(CommonSearchFilterModel searchFilter)
        {
            {


                try
                {
                    var result = buildUserPagedModel(searchFilter);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return Problem(ex.GetBaseException().ToString());
                }
            }
        }
        public class PaginationModel
        {
            public string? CurrentPage { get; set; }
            public string? NextPage { get; set; }
            public string? PrevPage { get; set; }
            public string? TotalPage { get; set; }
            public string? PageSize { get; set; }
            public string? TotalRecord { get; set; }
            public List<TblUsersModel_List> items { get; set; }


        }
        public partial class TblUsersModel_List
        {
            public int Id { get; set; }

            public string Username { get; set; }

            public string Password { get; set; }

            public string Fullname { get; set; }

            public string Fname { get; set; }

            public string Lname { get; set; }

            public string Mname { get; set; }

            public string Email { get; set; }

            public string Gender { get; set; }

            public string EmployeeId { get; set; }

            public string Jwtoken { get; set; }

            public string FilePath { get; set; }

            public int? Active { get; set; }

            public string Cno { get; set; }

            public string Address { get; set; }

            public int? Status { get; set; }
            public string? StatusName { get; set; }

            public string? DateCreated { get; set; }

            public string? DateUpdated { get; set; }

            public bool DeleteFlag { get; set; }

            public string CreatedBy { get; set; }

            public string UpdatedBy { get; set; }

            public string? DateDeleted { get; set; }

            public string DeletedBy { get; set; }

            public string? DateRestored { get; set; }

            public string RestoredBy { get; set; }

            public int? CenterId { get; set; }
            public string CenterName { get; set; }

            public bool? AgreementStatus { get; set; }

            public string RememberToken { get; set; }
            public string UserType { get; set; }
            public string UserTypeCode { get; set; }
            public string UserTypeName { get; set; }
            public ICollection<UserTypeAction_Model>? userAccessModels { get; set; } = new List<UserTypeAction_Model>();
        }
        private List<PaginationModel> buildUserPagedModel(CommonSearchFilterModel searchFilter)
        {
            var items = (dynamic)null;
            int totalItems = 0;
            int totalPages = 0;
            string page_size = searchFilter.pageSize == 0 ? "10" : searchFilter.pageSize.ToString();
            if (searchFilter.searchParam == null || searchFilter.searchParam == string.Empty)
            {

                var userlist = dbmet.getUserList().Where(a => a.Status == 3 || a.Status == 4).ToList();
                totalItems = userlist.Count;
                totalPages = (int)Math.Ceiling((double)totalItems / int.Parse(page_size.ToString()));
                items = userlist.Skip((searchFilter.page - 1) * int.Parse(page_size.ToString())).Take(int.Parse(page_size.ToString())).ToList();
            }
            else
            {
                var userlist = dbmet.getUserList().Where(a => a.Username == searchFilter.searchParam || a.Fname == searchFilter.searchParam ||
                a.Lname == searchFilter.searchParam || a.Mname == searchFilter.searchParam && a.Status == 3 || a.Status == 4).ToList();
                totalItems = userlist.Count;
                totalPages = (int)Math.Ceiling((double)totalItems / int.Parse(page_size.ToString()));
                items = userlist.Skip((searchFilter.page - 1) * int.Parse(page_size.ToString())).Take(int.Parse(page_size.ToString())).ToList();
            }

            var result = new List<PaginationModel>();
            var item = new PaginationModel();
            int pages = searchFilter.page == 0 ? 1 : searchFilter.page;
            item.CurrentPage = searchFilter.page == 0 ? "1" : searchFilter.page.ToString();

            int page_prev = pages - 1;
            //int t_record = int.Parse(items.Count.ToString()) / int.Parse(page_size);

            double t_records = Math.Ceiling(double.Parse(totalItems.ToString()) / double.Parse(page_size));
            int page_next = searchFilter.page >= t_records ? 0 : pages + 1;
            item.NextPage = items.Count % int.Parse(page_size) >= 0 ? page_next.ToString() : "0";
            item.PrevPage = pages == 1 ? "0" : page_prev.ToString();
            item.TotalPage = t_records.ToString();
            item.PageSize = page_size;
            item.TotalRecord = totalItems.ToString();
            item.items = items;
            result.Add(item);

            return result;
        }
        // GET: UserManagemetn/search/5
        [HttpGet("{username}")]
        public async Task<ActionResult<IEnumerable<UserResponseModel>>> search(string username)
        {
            try
            {
                var filter = new Dictionary<string, object>();
                filter.Add("username", username);
                List<TblUsersModel> userList = await buildUserManagementSearchQuery(filter).ToListAsync();

                if (userList.Count == 0)
                {
                    return Conflict("No records found!");
                }

                List<UserResponseModel> userResponseModels = convertUserListToResponseModelList(userList);

                return Ok(userResponseModels);
            }
            catch (Exception ex)
            {
                return Problem(ex.GetBaseException().ToString());
            }
        }
        [HttpGet]
        [Route("/UserManagement/useraccess/list/{username}")]
        public async Task<IActionResult> list(string username)
        {
            var userModel = await _context.TblUsersModels
                .Include(user => user.userAccessModels)
                .ThenInclude(userAccessModel => userAccessModel.userAccess)
                .Where(user => user.Username.Equals(username))
                .FirstOrDefaultAsync();

            if (userModel == null)
            {
                return Problem("Username does not exists!");
            }

            var userAccessListModel = populateUserAccessListModel(userModel);
            return Ok(userAccessListModel);
        }
        private UserAccessListModel populateUserAccessListModel(TblUsersModel usersModel)
        {
            var userAccessModels = new UserAccessListModel();
            userAccessModels.username = usersModel.Username;

            var userAccessList = new Dictionary<string, List<int>>();
            foreach (UserAccessModel userAccessModel in usersModel.userAccessModels)
            {

                var userAccessTypeList = new List<int>();
                foreach (UserAccessType userAccessType in userAccessModel.userAccess)
                {
                    userAccessTypeList.Add(userAccessType.Code);
                }

                userAccessList.Add(userAccessModel.module, userAccessTypeList);
            }

            userAccessModels.userAccessList = userAccessList;
            return userAccessModels;
        }

        // GET: usermanagement/useraccess/update/{username}
        [HttpPut]
        [Route("/UserManagement/useraccess/update/{username}")]
        public async Task<IActionResult> update(string username, UserAccessListModel userAccessListModel)
        {
            var userModel = await _context.TblUsersModels
                .Include(user => user.userAccessModels)
                .ThenInclude(userAccessModel => userAccessModel.userAccess)
                .Where(user => user.Username.Equals(username))
                .FirstOrDefaultAsync();

            if (userModel == null)
            {
                return Problem("Username does not exists!");
            }

            userModel.userAccessModels.Clear();
            userModel.userAccessModels.AddRange(populateUserAccessList(userAccessListModel.userAccessList));
            _context.Entry(userModel).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok("Update Successful!");
        }
        // PUT: UserManagement/update/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> update(int id, UserUpdateModel userUpdateModel)
        {
            //DataTable userRecord = db.SelectDb_WithParamAndSorting(QueryBuilder.buildUserSearchQueryById(), null, populateSqlParameters(id));
            var filter = new Dictionary<string, object>();
            filter.Add("Id", id);
            var userModel = await buildUserManagementSearchQuery(filter).FirstOrDefaultAsync();

            if (userModel == null)
            {
                return Conflict("No records matched!");
            }

            DataTable userDuplicateCheck = db.SelectDb_WithParamAndSorting(QueryBuilder.buildUserDuplicateCheckUpdateQuery(), null, populateSqlParameters(id, userUpdateModel));

            // check for duplication
            if (userDuplicateCheck.Rows.Count > 0)
            {
                return Conflict("Entity already exists");
            }

            try
            {
                userModel.userAccessModels.Clear();
                populateUser(userModel, userUpdateModel);
                populateUserAccess(userModel, userUpdateModel);
                _context.Entry(userModel).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok("Update Successful!");
            }
            catch (Exception ex)
            {

                return Problem(ex.GetBaseException().ToString());
            }
        }
        private void populateUserAccess(TblUsersModel userModel, UserUpdateModel updateModel)
        {
            userModel.userAccessModels.AddRange(populateUserAccessList(updateModel.userAccess));
        }

        private List<UserAccessModel> populateUserAccessList(Dictionary<string, List<int>> userAccessModelList)
        {
            var userAccessModels = new List<UserAccessModel>();

            foreach (var access in userAccessModelList)
            {
                var userAccessTypeList = new List<UserAccessType>();
                foreach (int userAccess in access.Value)
                {
                    var userAccessType = new UserAccessType()
                    {
                        Code = userAccess
                    };
                    _context.Attach(userAccessType);

                    userAccessTypeList.Add(userAccessType);
                }

                var userAccessModel = new UserAccessModel()
                {
                    module = access.Key
                };

                _context.Attach(userAccessModel);

                userAccessModel.userAccess.AddRange(userAccessTypeList);
                userAccessModels.Add(userAccessModel);
            }
            return userAccessModels;
        }
        private void populateUser(TblUsersModel userModel, UserUpdateModel userUpdateModel)
        {
            userModel.Username = userUpdateModel.Username;
            userModel.Password = Cryptography.Encrypt(userUpdateModel.Password);
            userModel.Fullname = userUpdateModel.Fullname;
            userModel.Fname = userUpdateModel.Fname;
            userModel.Lname = userUpdateModel.Lname;
            userModel.Mname = userUpdateModel.Mname;
            userModel.Email = userUpdateModel.Email;
            userModel.Gender = userUpdateModel.Gender;
            userModel.EmployeeId = userUpdateModel.EmployeeId;
            userModel.Active = userUpdateModel.Active;
            userModel.Cno = userUpdateModel.Cno;
            userModel.Address = userUpdateModel.Address;
            userModel.CenterId = userUpdateModel.CenterId;
            userModel.AgreementStatus = userUpdateModel.AgreementStatus;
        }

        // POST: UserManagement/delete/5
        [HttpPost]
        public async Task<IActionResult> delete(DeletionModel deletionModel)
        {
            DataTable userRecord = db.SelectDb_WithParamAndSorting(QueryBuilder.buildUserSearchQueryById(), null, populateSqlParameters(deletionModel.id));

            if (userRecord.Rows.Count == 0)
            {
                return Conflict("No records found!");
            }

            var userModel = convertDataRowToUser(userRecord.Rows[0]);

            try
            {
                userModel.DeleteFlag = true;
                userModel.DateDeleted = DateTime.Now;
                userModel.DeletedBy = deletionModel.deletedBy;
                userModel.DateRestored = null;
                userModel.RestoredBy = "";
                _context.Entry(userModel).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok("Deletion Successful!");
            }
            catch (Exception ex)
            {

                return Problem(ex.GetBaseException().ToString());
            }
        }

        // POST: UserManagemetn/restore/
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> restore(RestorationModel restorationModel)
        {


            DataTable userRecord = db.SelectDb_WithParamAndSorting(QueryBuilder.buildUserDeletedSearchQueryById(), null, populateSqlParameters(restorationModel.id));

            if (userRecord.Rows.Count == 0)
            {
                return Conflict("No deleted records found!");
            }

            var userModel = convertDataRowToUser(userRecord.Rows[0]);

            try
            {
                userModel.DeleteFlag = !userModel.DeleteFlag;
                userModel.DateDeleted = null;
                userModel.DeletedBy = "";
                userModel.DateRestored = DateTime.Now;
                userModel.RestoredBy = restorationModel.restoredBy;

                _context.Entry(userModel).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok("Restoration Successful!");
            }
            catch (Exception ex)
            {

                return Problem(ex.GetBaseException().ToString());
            }
        }


        private SqlParameter[] populateSqlParameters(CommonSearchFilterModel searchFilter)
        {

            var sqlParameters = new List<SqlParameter>();

            if (searchFilter.searchParam != null && searchFilter.searchParam != "")
            {
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "SearchParam",
                    Value = searchFilter.searchParam ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.VarChar,
                });
            }

            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParameters(int id)
        {
            var sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Id",
                Value = id,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });
            return sqlParameters.ToArray();
        }

        private SqlParameter[] populateSqlParameters(string username)
        {
            var sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Username",
                Value = username ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });
            return sqlParameters.ToArray();
        }
        private SqlParameter[] populateSqlParameters(int id, UserUpdateModel userUpdateModel)
        {
            var sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Id",
                Value = id,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Username",
                Value = userUpdateModel.Username,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Fullname",
                Value = userUpdateModel.Fullname,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Fname",
                Value = userUpdateModel.Fname,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Lname",
                Value = userUpdateModel.Lname,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Mname",
                Value = userUpdateModel.Mname,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "Email",
                Value = userUpdateModel.Email,
                SqlDbType = System.Data.SqlDbType.VarChar,
            });

            return sqlParameters.ToArray();
        }

        private List<UserPagedModel> buildUserPagedModel(CommonSearchFilterModel searchFilter, List<TblUsersModel> userList)
        {
            int pagesize = searchFilter.pageSize == 0 ? 10 : searchFilter.pageSize;
            int page = searchFilter.page == 0 ? 1 : searchFilter.page;
            var items = (dynamic)null;

            int totalItems = userList.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pagesize);
            items = userList.AsEnumerable().Skip((page - 1) * pagesize).Take(pagesize).ToList();

            List<UserResponseModel> userResponseModels = convertUserListToResponseModelList(userList);

            var result = new List<UserPagedModel>();
            var item = new UserPagedModel();

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
            item.items = userResponseModels;
            result.Add(item);

            return result;
        }

        private List<TblUsersModel> convertDataRowToUserList(List<DataRow> dataRowList)
        {
            var userList = new List<TblUsersModel>();

            foreach (DataRow dataRow in dataRowList)
            {
                var user = DataRowToObject.ToObject<TblUsersModel>(dataRow);
                userList.Add(user);
            }

            return userList;
        }

        private TblUsersModel convertDataRowToUser(DataRow dataRow)
        {
            return DataRowToObject.ToObject<TblUsersModel>(dataRow);
        }

        private List<UserResponseModel> convertUserListToResponseModelList(List<TblUsersModel> userList)
        {
            var userResponseModels = new List<UserResponseModel>();

            foreach (TblUsersModel user in userList)
            {
                var userResponseModel = new UserResponseModel()
                {
                    Id = user.Id,
                    FilePath = user.FilePath,
                    Username = user.Username,
                    Fullname = user.Fullname,
                    Fname = user.Fname,
                    Lname = user.Lname,
                    Mname = user.Mname,
                    Email = user.Email,
                    Gender = user.Gender,
                    EmployeeId = user.EmployeeId,
                    Active = user.Active,
                    Cno = user.Cno,
                    Address = user.Address,
                    CenterId = user.CenterId,
                    AgreementStatus = user.AgreementStatus,
                    UserType = user.UserType
                };
                userResponseModels.Add(userResponseModel);
            }

            return userResponseModels;
        }

    }
}
