public interface ILicenseRepository {
    void InitDatabase();
    License AddLicense(License license);
    IEnumerable<License> GetLicenseStatuses();
    License RentLicense(string licenseKey, string client);
    void UnrentLicense(string licenseKey);
    License GetLicense(string licenseKey);
    License GetClientLease(string client);
}