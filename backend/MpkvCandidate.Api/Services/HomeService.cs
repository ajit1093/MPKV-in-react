using Dapper;
using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;

namespace MpkvCandidate.Api.Services
{
    public interface IHomeService
    {
        HomePageResponse GetHomePageData(short regionId = 1);
    }

    public class HomeService : IHomeService
    {
        private readonly DbAccess _db;
        private readonly IConfiguration _config;

        public HomeService(DbAccess db, IConfiguration config)
        {
            _db     = db;
            _config = config;
        }

        public HomePageResponse GetHomePageData(short regionId = 1)
        {
            var response = new HomePageResponse
            {
                WebsiteHeader    = _config["AppSettings:WebsiteHeader"]    ?? "Online Agriculture Diploma Admissions - 2026",
                HelplineMobileNo = _config["AppSettings:HelplineMobileNo"] ?? "+91-8806612998"
            };

            // ── 1. Is registration open? (Base_IsNewCandidateRegistrationStarted) ──
            try
            {
                var result = _db.ExecuteScalar("Base_IsNewCandidateRegistrationStarted");
                response.IsRegistrationOpen = result != null && Convert.ToBoolean(result);
            }
            catch { response.IsRegistrationOpen = false; }

            // ── 2. Nav menu (Menu_GetMenu) ────────────────────────────────────────
            try
            {
                var menuParam = new DynamicParameters();
                menuParam.Add("@RegionID",    regionId);
                menuParam.Add("@UserTypeID",  0);
                menuParam.Add("@UserLoginID", "");
                menuParam.Add("@Language",    "");

                var menuDt = _db.GetDataTable("Menu_GetMenu", menuParam);
                var allMenus = new List<MenuItemDto>();

                if (menuDt != null)
                {
                    foreach (System.Data.DataRow row in menuDt.Rows)
                    {
                        allMenus.Add(new MenuItemDto
                        {
                            MenuId      = Convert.ToInt32(row["MenuID"]),
                            ParentMenuId= Convert.ToInt32(row["ParentMenuID"]),
                            LinkName    = row["LinkName"]?.ToString() ?? "",
                            LinkUrl     = row["LinkURL"]?.ToString()  ?? "",
                            Target      = row["Target"]?.ToString()   ?? "",
                            SeqNo       = Convert.ToInt32(row["SeqNo"])
                        });
                    }
                }

                // Build parent → children tree
                var parents = allMenus
                    .Where(m => m.ParentMenuId == 0)
                    .OrderBy(m => m.SeqNo)
                    .ToList();

                foreach (var parent in parents)
                {
                    parent.Children = allMenus
                        .Where(m => m.ParentMenuId == parent.MenuId)
                        .OrderBy(m => m.SeqNo)
                        .ToList();
                }

                response.MenuItems = parents;
            }
            catch { /* menu fails silently */ }

            // ── 3. Notifications / News / Downloads / Announcements / Popup ──────
            // SP: Administration_GetNotificationListForDisplay
            // NotificationCategoryID:
            //   1 = Announcement (marquee ticker)
            //   2 = News tab
            //   3 = Notifications tab
            //   4 = Downloads tab
            //  11 = Popup modal
            try
            {
                var notifParam = new DynamicParameters();
                notifParam.Add("@RegionID", regionId);
                notifParam.Add("@Language", "");

                var notifDt = _db.GetDataTable("Administration_GetNotificationListForDisplay", notifParam);

                if (notifDt != null)
                {
                    bool popupSet = false;

                    foreach (System.Data.DataRow row in notifDt.Rows)
                    {
                        int categoryId = Convert.ToInt32(row["NotificationCategoryID"]);
                        bool isNew = Convert.ToInt16(row["DisplayNewImage"]) == 1;

                        // Get publish date — SP returns PublishDateTime column
                        string publishDate = "";
                        if (notifDt.Columns.Contains("PublishDateTime") && row["PublishDateTime"] != DBNull.Value)
                            publishDate = Convert.ToDateTime(row["PublishDateTime"]).ToString("dd MMM yyyy");
                        else if (notifDt.Columns.Contains("PublishDate") && row["PublishDate"] != DBNull.Value)
                            publishDate = Convert.ToDateTime(row["PublishDate"]).ToString("dd MMM yyyy");
                        else if (notifDt.Columns.Contains("CreatedDate") && row["CreatedDate"] != DBNull.Value)
                            publishDate = Convert.ToDateTime(row["CreatedDate"]).ToString("dd MMM yyyy");

                        var dto = new NotificationDto
                        {
                            Title          = row["NotificationTitle"]?.ToString() ?? "",
                            TextContent    = row["TextContent"]?.ToString()       ?? "",
                            FileContentUrl = row["FileContentURL"]?.ToString()    ?? "",
                            ContentType    = row["ContentType"]?.ToString()       ?? "T",
                            IsNew          = isNew,
                            PublishDate    = publishDate,
                            CategoryId     = categoryId
                        };

                        switch (categoryId)
                        {
                            case 1:  response.Announcements.Add(dto);  break;
                            case 2:  response.News.Add(dto);           break;
                            case 3:  response.Notifications.Add(dto);  break;
                            case 4:  response.Downloads.Add(dto);      break;
                            case 11:
                                if (!popupSet)
                                {
                                    response.Popup = new PopupDto
                                    {
                                        Header = dto.Title,
                                        Text   = dto.TextContent
                                    };
                                    popupSet = true;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetHomePageData notifications error: {ex.Message}");
            }

            return response;
        }
    }
}
