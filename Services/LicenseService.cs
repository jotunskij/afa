public class LicenseService : ILicenseService {

    private readonly ILicenseRepository repository;
    private readonly ILogger<LicenseService> logger;

    public LicenseService(ILicenseRepository repository, ILogger<LicenseService> logger) {
        this.repository = repository;
        this.logger = logger;
    }

    public License AddLicense(string licenseKey)
    {
        var license = new License() {
            licenseKey = licenseKey
        };
        return repository.AddLicense(license);
    }

    public IEnumerable<License> GetLicenseStatuses() {
        return repository.GetLicenseStatuses();
    }

    public void InitLicenseDatabase() {
        repository.InitDatabase();
    }

    public License RentLicense(string licenseKey, string client) {
        var existingLicense = repository.GetLicense(licenseKey);

        if (existingLicense.licenseKey == null) {
            throw new LicenseException("License does not exist");
        }
        
        if (existingLicense.rentedBy != null && existingLicense.rentedBy != client) {
            throw new LicenseException("License already rented by other client");
        }

        var existingLease = repository.GetClientLease(client);

        if (existingLease.licenseKey != null) {
            throw new LicenseException("Client already has active lease");
        }

        var license = repository.RentLicense(licenseKey, client);
        logger.LogInformation($"Client {client} leased {licenseKey}");

        // Schedule removal of license rent
        // For production: replace with more appropriate framework
        // For ~15sec: quartz or ihostedservice?
        // For ==15sec: something else
        Task.Run(() => UnrentLicense(licenseKey));

        return license;
    }

    private async Task UnrentLicense(string licenseKey) {
        await Task.Delay(15 * 1000);
        repository.UnrentLicense(licenseKey);
        logger.LogInformation($"License {licenseKey} lease expired");
    }

}