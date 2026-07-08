using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace TL.Helpers;

public static class PdfResponse
{
    public static IActionResult File(ControllerBase controller, byte[] bytes, string filename)
    {
        var safe = SanitizeFilename(filename);

        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(safe);
        controller.Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();
        controller.Response.Headers[HeaderNames.ContentType] = "application/pdf";
        controller.Response.Headers["X-Content-Type-Options"] = "nosniff";
        controller.Response.Headers["Accept-Ranges"] = "bytes";
        controller.Response.ContentLength = bytes.Length;

        return new FileContentResult(bytes, "application/pdf")
        {
            EnableRangeProcessing = true,
        };
    }

    static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return "document.pdf";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = filename.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var safe = new string(chars).Trim();
        if (safe.Length == 0) safe = "document";
        if (!safe.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            safe += ".pdf";
        return safe;
    }
}
