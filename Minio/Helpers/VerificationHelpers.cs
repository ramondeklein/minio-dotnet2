namespace Minio.Helpers;

public static class VerificationHelpers
{
    // see https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucketnamingrules.html
    public static bool VerifyBucketName(string bucketName)
    {
        if (bucketName == null) 
            throw new ArgumentNullException(nameof(bucketName));
        
        // Bucket names must be between 3 (min) and 63 (max) characters long
        if (bucketName.Length < 3 || bucketName.Length > 63) 
            return false;
        
        for (var i=0; i<bucketName.Length; ++i)
        {
            var ch = bucketName[i];
            
            // Bucket names can consist only of lowercase letters, numbers, dots (.), and hyphens (-)
            if (!char.IsLower(ch) && !char.IsDigit(ch) && ch != '.' && ch != '-')
                return false;
            
            // Bucket names must begin and end with a letter or number
            if ((i == 0 || i == bucketName.Length - 1) && !char.IsLower(ch) && !char.IsDigit(ch))
                return false;
            
            // Bucket names must not contain two adjacent periods 
            if (i > 0 && ch == '.' && bucketName[i - 1] == '.')
                return false;
        }
        
        // Bucket names must not start with the prefix xn--
        if (bucketName.StartsWith("xn--", StringComparison.Ordinal)) return false;

        // Bucket names must not start with the prefix sthree- and the prefix sthree-configurator
        if (bucketName.StartsWith("sthree-", StringComparison.Ordinal)) return false;

        // Bucket names must not end with the suffix -s3alias
        if (bucketName.EndsWith("-s3alias", StringComparison.Ordinal)) return false;

        // Bucket names must not end with the suffix --ol-s3
        if (bucketName.EndsWith("--ol-s3", StringComparison.Ordinal)) return false;

        return true;
    }
}