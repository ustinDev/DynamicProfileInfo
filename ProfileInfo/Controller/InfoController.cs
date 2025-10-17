using Microsoft.AspNetCore.Mvc;
using ProfileInfo.Services.Interfaces;

namespace ProfileInfo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeController : ControllerBase
    {
        private readonly ICatFactService _catFactService;
        private readonly ILogger<MeController> _logger;

        public MeController(ICatFactService catFactService, ILogger<MeController> logger)
        {
            _catFactService = catFactService;
            _logger = logger;
        }

        [HttpGet]
        [Route("/me")]
        public async Task<IActionResult> GetProfileAsync()
        {
            try
            {
                var catFact = await _catFactService.GetRandomCatFactAsync();

                var response = new
                {
                    status = "success",
                    user = new
                    {
                        email = "ustinsteve@gmail.com",
                        name = "Stephen Odiase",
                        stack = "C# / ASP.NET Core"
                    },
                    timestamp = DateTime.UtcNow.ToString("o"), // ISO-8601 UTC
                    fact = catFact
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building /me response");
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Unable to fetch cat fact at this time."
                });
            }
        }
    }
}
