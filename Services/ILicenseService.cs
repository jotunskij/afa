public interface ILicenseService {
    void InitLicenseDatabase();
    License AddLicense(string licenseKey);
    IEnumerable<License> GetLicenseStatuses();
    License RentLicense(string licenseKey, string client);
}