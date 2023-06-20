using Microsoft.Data.Sqlite;

// I opted to use Sqlite because of:
// 1) Simple to embed and low footprint
// 2) FOSS (not everyone uses MSSQL with .NET)
// If we really wanted to use LINQ we could probably do so
// against Sqlite with a little bit of more work, but
// since the repository interface is as simple as it is
// I opted not to.

public class SqliteRepository : ILicenseRepository {

    private readonly ILogger _logger;

    public SqliteRepository(ILogger logger) {
        _logger = logger;
    }

    public void InitDatabase() {
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

    public License AddLicense(License license) {
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
                return license;
            } catch (SqliteException) {
                _logger.LogWarning("Attempted to insert duplicate license key");
                throw new LicenseException("Attempted to insert duplicate license key");
            }
        }
    }

    public IEnumerable<License> GetLicenseStatuses() {
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
            return licenses;
        }
    }

    public License RentLicense(string licenseKey, string client)
    {
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
            return new License() {
                licenseKey = licenseKey,
                rentedUntil = rentedUntil,
                rentedBy = client
            };
        }
    }

    public void UnrentLicense(string licenseKey) {
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