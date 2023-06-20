public class LicenseException : Exception { 

    public readonly string ErrorMessage;

    public LicenseException(string message) {
        this.ErrorMessage = message;
    }

}