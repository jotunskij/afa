using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace afa.Controllers;

[ApiController]
public class LicenseController : ControllerBase
{

    private readonly ILogger<LicenseController> _logger;
    private readonly ILicenseService licenseService;

    public LicenseController(ILogger<LicenseController> logger, ILicenseService licenseService)
    {
        _logger = logger;
        this.licenseService = licenseService;
        InitDatabase();
    }

    private void InitDatabase() {
        licenseService.InitLicenseDatabase();
    }

    [HttpPost("license", Name = "Add license")]
    public ActionResult<License> AddNewLicense(string licenseKey) {
        try {
            var license = licenseService.AddLicense(licenseKey);
            return CreatedAtAction(nameof(License), new { id = licenseKey }, license);
        } catch (LicenseException le) {
            return BadRequest(new { error = le.ErrorMessage });
        }
    }

    [HttpGet("licenses", Name = "Get license statuses")]
    public ActionResult<IEnumerable<License>> GetLicenseStatuses()
    {
        return Ok(licenseService.GetLicenseStatuses());
    }

    [HttpGet("rent", Name = "Rent license")]
    public ActionResult<License> RentLicense(string licenseKey, string client) {
        try {
            return Ok(licenseService.RentLicense(licenseKey, client));
        } catch (LicenseException le) {
            return BadRequest(new { error = le.ErrorMessage });
        }
    }


}
