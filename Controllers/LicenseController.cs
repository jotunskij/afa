using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace afa.Controllers;

[ApiController]
public class LicenseController : ControllerBase
{

    private readonly ILogger<LicenseController> _logger;

    public LicenseController(ILogger<LicenseController> logger)
    {
        _logger = logger;
        InitDatabase();
    }

    private void InitDatabase() {
        using (var conn = new SqliteConnection("Data Source=database.db")) {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Licenses (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    licenseKey VARCHAR(10) UNIQUE NOT NULL,
                    rentedUntil DATETIME NULL,
                    rentedBy VARCHAR(50) NULL
                );
            ";
            cmd.ExecuteNonQuery();
        }
    }

    [HttpPost("license", Name = "Add license")]
    public ActionResult<License> AddNewLicense(string licenseKey) {
        var license = new License() {
            licenseKey = licenseKey
        };
        using (var conn = new SqliteConnection("Data Source=database.db")) {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Licenses (licenseKey, rentedUntil, rentedBy) 
                VALUES ($licenseKey, NULL, NULL);
            ";
            cmd.Parameters.AddWithValue("$licenseKey", license.licenseKey);
            try {
                cmd.ExecuteNonQuery();
                return CreatedAtAction(nameof(License), license);
            } catch (SqliteException) {
                _logger.LogWarning("Attempted to insert duplicate license key");
                return BadRequest(license);
            }
        }
    }

    [HttpGet("licenses", Name = "Get license statuses")]
    public ActionResult<IEnumerable<License>> GetLicenseStatuses()
    {
        using (var conn = new SqliteConnection("Data Source=database.db")) {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT licenseKey, rentedUntil
                FROM Licenses;
            ";
            var licenses = new List<License>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    licenses.Add(new License() {
                        licenseKey = reader.GetString(0),
                        rentedUntil = reader.IsDBNull(1) ? null : reader.GetDateTime(1)
                    });
                }
            }
            return Ok(licenses);
        }
    }

    [HttpGet("rent", Name = "Rent license")]
    public ActionResult<License> RentLicense(string licenseKey, string client) {
        using (var conn = new SqliteConnection("Data Source=database.db")) {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Licenses 
                SET rentedUntil = $rentedUntil, rentedBy = $client
                WHERE licenseKey = $licenseKey;
            ";
            var rentedUntil = DateTime.Now.AddSeconds(15);
            cmd.Parameters.AddWithValue("$rentedUntil", rentedUntil);
            cmd.Parameters.AddWithValue("$client", client);
            cmd.Parameters.AddWithValue("$licenseKey", licenseKey);
            cmd.ExecuteNonQuery();
            var license = new License() {
                licenseKey = licenseKey,
                rentedUntil = rentedUntil,
                rentedBy = client
            };

            // Schedule removal of license rent
            Task.Run(() => UnrentLicense(licenseKey));

            return Ok(license);
        }
    }

    private async Task UnrentLicense(string licenseKey) {
        await Task.Delay(15 * 1000);
        using (var conn = new SqliteConnection("Data Source=database.db")) {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Licenses 
                SET rentedUntil = NULL, rentedBy = NULL
                WHERE licenseKey = $licenseKey;
            ";
            cmd.Parameters.AddWithValue("$licenseKey", licenseKey);
            cmd.ExecuteNonQuery();
        }
    }
}
