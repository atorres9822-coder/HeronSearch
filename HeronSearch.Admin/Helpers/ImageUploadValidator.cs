namespace HeronSearch.Admin.Helpers;

// Checks, saves, and deletes item photos so only real JPG, PNG, and WEBP images reach wwwroot/uploads.
public static class ImageUploadValidator
{
    // The largest allowed photo size in bytes (5 MB).
    public const long MaxBytes = 5_242_880;

    // How many leading bytes the signature check needs (WebP needs 12).
    public const int HeaderLength = 12;

    // The allowed extensions as one comma-separated list, for the file input's accept attribute and the client rules.
    public const string AllowedExtensionsText = ".jpg,.jpeg,.png,.webp";

    // The allowed extensions, lowercase with the leading dot.
    public static readonly IReadOnlyList<string> AllowedExtensions = Array.AsReadOnly(AllowedExtensionsText.Split(','));

    // Shown when the selected file has 0 bytes.
    public const string EmptyMessage = "The selected photo is empty.";

    // Shown when the file is larger than MaxBytes.
    public const string TooLargeMessage = "Photo must be 5 MB or smaller.";

    // Shown when the extension is not allowed or the file content does not match the extension.
    public const string InvalidTypeMessage = "Photo must be a JPG, PNG, or WEBP image.";

    // Shown next to the Photo field when saving a valid photo fails, for example because of a disk error.
    public const string SaveFailedMessage = "The photo could not be saved. Try again.";

    // The start of every stored photo path, as in "/uploads/<name>.jpg".
    public const string UploadsUrlPrefix = "/uploads/";

    // The name of the photo folder inside wwwroot.
    private const string UploadsFolderName = "uploads";

    // The longest stored file name that ResolveUploadPath accepts (the usual file system limit).
    private const int MaxFileNameLength = 255;

    // Characters a stored photo name never contains: both slashes, the drive colon, and the system's invalid file name characters.
    private static readonly char[] ForbiddenNameCharacters = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':' }).ToArray();

    // JPEG files start with the bytes FF D8 FF.
    private static ReadOnlySpan<byte> JpegSignature => new byte[] { 0xFF, 0xD8, 0xFF };

    // PNG files start with the bytes 89 50 4E 47 0D 0A 1A 0A.
    private static ReadOnlySpan<byte> PngSignature => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    // WebP files start with "RIFF" (52 49 46 46) at byte 0.
    private static ReadOnlySpan<byte> RiffSignature => new byte[] { 0x52, 0x49, 0x46, 0x46 };

    // WebP files have "WEBP" (57 45 42 50) at byte 8.
    private static ReadOnlySpan<byte> WebpSignature => new byte[] { 0x57, 0x45, 0x42, 0x50 };

    // Returns null when the photo is acceptable, or the exact error message. The checks run in order: empty, size, then type.
    public static string? Check(string fileName, long length, ReadOnlySpan<byte> header)
    {
        if (length <= 0)
        {
            return EmptyMessage;
        }

        if (length > MaxBytes)
        {
            return TooLargeMessage;
        }

        var extension = GetLowerExtension(fileName);
        return MatchesSignature(extension, header) ? null : InvalidTypeMessage;
    }

    // Reads the first 12 bytes of the uploaded file and returns Check's answer: null when valid, otherwise the error message.
    public static async Task<string?> ValidateAsync(IFormFile file)
    {
        var header = new byte[HeaderLength];
        int bytesRead;
        await using (var stream = file.OpenReadStream())
        {
            // Keeps reading until the header is full or a short file ends.
            bytesRead = await stream.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false);
        }

        return Check(file.FileName, file.Length, header.AsSpan(0, bytesRead));
    }

    // Builds a new stored file name: a GUID in "N" format (32 hex digits) plus the lowercase extension. The original name is never used.
    public static string BuildStoredName(string extension)
    {
        return Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
    }

    // Saves a validated photo in wwwroot/uploads under a new GUID name and returns its URL path, "/uploads/{name}".
    public static async Task<string> SaveAsync(IFormFile file, string webRootPath)
    {
        var extension = GetLowerExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            // Callers validate first; this keeps every other file type out of the public uploads folder.
            throw new ArgumentException(InvalidTypeMessage, nameof(file));
        }

        // Creates wwwroot/uploads when it is missing (nothing happens when it already exists).
        var uploadsFolder = GetUploadsFolder(webRootPath);
        Directory.CreateDirectory(uploadsFolder);

        var storedName = BuildStoredName(extension);
        var fullPath = Path.Combine(uploadsFolder, storedName);

        // CreateNew never overwrites an existing file.
        var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            await using (target)
            {
                await file.CopyToAsync(target);
            }
        }
        catch
        {
            // Removes the partly written file so a failed save leaves nothing behind, then passes the error on.
            TryDeleteFile(fullPath);
            throw;
        }

        return UploadsUrlPrefix + storedName;
    }

    // Returns the full path of a stored photo when imagePath is "/uploads/<name>" and the file sits directly in wwwroot/uploads; otherwise null.
    public static string? ResolveUploadPath(string webRootPath, string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(webRootPath)
            || imagePath == null
            || !imagePath.StartsWith(UploadsUrlPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var name = imagePath.Substring(UploadsUrlPrefix.Length);
        if (!IsPlainFileName(name))
        {
            return null;
        }

        var uploadsFolder = GetUploadsFolder(webRootPath);
        var candidate = Path.Combine(uploadsFolder, name);
        var fullPath = Path.GetFullPath(candidate);

        // The system must not reshape the path (for example by trimming trailing dots), and its parent must be the uploads folder.
        var staysInUploads = fullPath == candidate
            && Path.GetDirectoryName(fullPath) == uploadsFolder
            && Path.GetFileName(fullPath) == name;
        return staysInUploads ? fullPath : null;
    }

    // Deletes a stored photo, but only a file directly inside wwwroot/uploads; returns true when a file was deleted.
    // Refused paths and missing files are ignored, and so is a locked file, because callers delete after saving the record.
    public static bool DeleteUploadedFile(string webRootPath, string? imagePath)
    {
        var fullPath = ResolveUploadPath(webRootPath, imagePath);
        if (fullPath == null || !File.Exists(fullPath))
        {
            return false;
        }

        return TryDeleteFile(fullPath);
    }

    // Returns true when the leading bytes match the signature that belongs to the lowercase extension.
    private static bool MatchesSignature(string extension, ReadOnlySpan<byte> header)
    {
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return header.StartsWith(JpegSignature);
            case ".png":
                return header.StartsWith(PngSignature);
            case ".webp":
                return header.Length >= HeaderLength
                    && header.StartsWith(RiffSignature)
                    && header.Slice(8, 4).SequenceEqual(WebpSignature);
            default:
                return false;
        }
    }

    // Returns the file name's extension in lowercase with its dot, or "" when there is none.
    private static string GetLowerExtension(string? fileName)
    {
        return Path.GetExtension(fileName ?? "").ToLowerInvariant();
    }

    // Returns the full, normalized path of the wwwroot/uploads folder.
    private static string GetUploadsFolder(string webRootPath)
    {
        return Path.GetFullPath(Path.Combine(webRootPath, UploadsFolderName));
    }

    // Returns true when the name is one plain file name: not empty, not "." or "..", and free of separators and invalid characters.
    private static bool IsPlainFileName(string name)
    {
        return name.Length > 0
            && name.Length <= MaxFileNameLength
            && name != "."
            && name != ".."
            && name.IndexOfAny(ForbiddenNameCharacters) < 0;
    }

    // Deletes a file and reports whether it worked; a locked or protected file stays in place instead of throwing.
    private static bool TryDeleteFile(string fullPath)
    {
        try
        {
            File.Delete(fullPath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
