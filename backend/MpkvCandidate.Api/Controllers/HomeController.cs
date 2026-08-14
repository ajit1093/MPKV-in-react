using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Services;

namespace MpkvCandidate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        /// <summary>
        /// Returns all home page data in one call:
        /// menu, notifications, news, downloads, announcements, popup, registration status.
        /// GET /api/home
        /// </summary>
        [HttpGet]
        public IActionResult GetHomeData([FromQuery] short regionId = 1)
        {
            var result = _homeService.GetHomePageData(regionId);
            return Ok(result);
        }
    }
}
