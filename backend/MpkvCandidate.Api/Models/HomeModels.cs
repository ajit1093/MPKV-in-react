namespace MpkvCandidate.Api.Models
{
    // ── Home Page ────────────────────────────────────────────────────────────

    public class HomePageResponse
    {
        public bool IsRegistrationOpen { get; set; }
        public string WebsiteHeader { get; set; } = string.Empty;
        public string HelplineMobileNo { get; set; } = string.Empty;
        public List<MenuItemDto> MenuItems { get; set; } = new();
        public List<NotificationDto> Announcements { get; set; } = new();   // marquee
        public List<NotificationDto> Notifications { get; set; } = new();   // tab 1
        public List<NotificationDto> News { get; set; } = new();            // tab 2
        public List<NotificationDto> Downloads { get; set; } = new();       // tab 3
        public PopupDto? Popup { get; set; }                                 // modal popup
    }

    public class NotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public string FileContentUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;  // T=text, F=file
        public bool IsNew { get; set; }
        public string PublishDate { get; set; } = string.Empty;
        public int CategoryId { get; set; }
    }

    public class MenuItemDto
    {
        public int MenuId { get; set; }
        public int ParentMenuId { get; set; }
        public string LinkName { get; set; } = string.Empty;
        public string LinkUrl { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int SeqNo { get; set; }
        public List<MenuItemDto> Children { get; set; } = new();
    }

    public class PopupDto
    {
        public string Header { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
