
using API_PCC.ApplicationModels;
using API_PCC.ApplicationModels.Common;
using API_PCC.Data;
using API_PCC.Manager;
using API_PCC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using Org.BouncyCastle.Utilities;
using System.Data;
using System.Drawing.Printing;
using System.Data;
using System.Data.SqlClient;
using API_PCC.Utils;
using API_PCC.EntityModels;
using static API_PCC.Controllers.UserManagementController;
using static API_PCC.Manager.DBMethods;
namespace API_PCC.Controllers
{
    [Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class AuditTrailController : ControllerBase
    {
        private readonly PCC_DEVContext _context;
        DbManager db = new DbManager();
        DBMethods dbmet = new DBMethods();
        public class BirthTypesSearchFilter
        {
            public string? BirthTypeCode { get; set; }
            public string? BirthTypeDesc { get; set; }
            public int page { get; set; }
            public int pageSize { get; set; }
        }

        public AuditTrailController(PCC_DEVContext context)
        {
            _context = context;
        }
        public class PaginationModel
        {
            public string? CurrentPage { get; set; }
            public string? NextPage { get; set; }
            public string? PrevPage { get; set; }
            public string? TotalPage { get; set; }
            public string? PageSize { get; set; }
            public string? TotalRecord { get; set; }
            public List<AuditTrailModel> items { get; set; }


        }
        // POST: BirthTypes/list
        [HttpPost]
        public async Task<ActionResult<IEnumerable<ABirthType>>> list(CommonSearchFilterModel searchFilter)
        {
            try
            {
                var result = buildUserPagedModel(searchFilter);
             //   var animal_details221 = _context.ABuffAnimals.Where(a=>a.Id == 3).FirstOrDefault();
                return Ok(result);

            }
            catch (Exception ex)
            {
                return Problem(ex.GetBaseException().ToString());
            }
        }

        private List<PaginationModel> buildUserPagedModel(CommonSearchFilterModel searchFilter)
        {
            var items = (dynamic)null;
            int totalItems = 0;
            int totalPages = 0;
            string page_size = searchFilter.pageSize == 0 ? "10" : searchFilter.pageSize.ToString();
            if (searchFilter.searchParam == null || searchFilter.searchParam == string.Empty)
            {

                var userlist = dbmet.GetAuditTrail().ToList();
                totalItems = userlist.Count;
                totalPages = (int)Math.Ceiling((double)totalItems / int.Parse(page_size.ToString()));
                items = userlist.Skip((searchFilter.page - 1) * int.Parse(page_size.ToString())).Take(int.Parse(page_size.ToString())).ToList();
            }
            else
            {
                var userlist = dbmet.GetAuditTrail().Where(a => a.Username.ToUpper().Contains(searchFilter.searchParam.ToUpper())).ToList();
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
    }
}
