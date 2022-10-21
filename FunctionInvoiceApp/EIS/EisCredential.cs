namespace EIS
{
    static public class EisCredential
    {
        // Set EIS Certification UserID and Password
        public const string USER_ID = "YOUR_USER_NAME";
        public const string PASSWORD = "YOUR_USER_PASSWORD";

        // Set Application Information
        public const string APPLICATION_ID = "YOUR_APPLICATION_ID";
        // Set EIS Certification Number
        public const string ACCREDITATION_ID = "YOUR_EIS_CERT_NUMBER";
        // Set Applicat Key - You can see or regenerate it on the setting page of your application in the EIS Certification portal.
        public const string APPLICATION_SECRET_KEY = "YOUR_APPLICATION_KEY";

        // EIS Public Key - You can download it on the download page of the EIS Certification portal.
        public const string PUBLIC_KEY = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAgMbSxoPRLi4P98qbfdFvwYCEf6l2QcKHhyE+m7Fh8OSqKqQFWud0+SSqydzYZzQYZIQ0hwZ/Vvd6StsEY80O7XC6ELVZ052s91PjAlh38TSzmJGy8ZZUYLsg8S2DzKaCpQ0ZmvphYf0ZB8ZoOXBTVPpg4cGBVbMZLdTtnXYxSegXhog6XBsIkAXmAWHwzJ0t6x0NbMnsfbHvFlqtUrsbwBc4BD+0rO3lJHPbDO4HEiMmrlM/bD/hL4uKzXv3jeXCkDbQdYsZZgI7tglu2Al/jB8VdMDJRJjsQf0Z5Ye3FdOsqp1v3SF3ENns8F/0A8xrrB/SuKcwO7Rvm2fjogoqqwIDAQAB";

        // Belows are for Invoice Issuance API
        public const string JWS_KEY_ID = "6VdQnBkKzC";
        public const string JWS_PRIVATE_KEY = "-----BEGIN PRIVATE KEY-----\n"
            // Please change the below string to your Digital Signing private key (You can generate Digital Signing Key-pair on the Setting page of your Application in the EIS Certification portal)
            + "YOUR_DIGITAL_SIGNING_KEY_PAIR_PRIVATE_KEY"
            + "\n-----END PRIVATE KEY-----\n";
    }
}