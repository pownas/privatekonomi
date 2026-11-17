namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for Optical Character Recognition (OCR) on receipt images
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Process a receipt image and extract text using OCR
    /// </summary>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <param name="fileName">Original file name (for format detection)</param>
    /// <returns>OCR result with extracted text and parsed data</returns>
    Task<OcrResult> ProcessReceiptImageAsync(Stream imageStream, string fileName);

    /// <summary>
    /// Parse OCR text to extract receipt information
    /// </summary>
    /// <param name="ocrText">Raw text from OCR</param>
    /// <returns>Parsed receipt data</returns>
    OcrReceiptData ParseReceiptText(string ocrText);

    /// <summary>
    /// Check if the OCR engine is available and properly configured
    /// </summary>
    /// <returns>True if OCR is available, false otherwise</returns>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// Result of OCR processing
/// </summary>
public class OcrResult
{
    public bool Success { get; set; }
    public string RawText { get; set; } = string.Empty;
    public OcrReceiptData? ParsedData { get; set; }
    public string? ErrorMessage { get; set; }
    public float Confidence { get; set; }
}

/// <summary>
/// Parsed receipt data from OCR text
/// </summary>
public class OcrReceiptData
{
    public string? Merchant { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTime? Date { get; set; }
    public string? ReceiptNumber { get; set; }
    public List<OcrLineItem> LineItems { get; set; } = new();
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// Line item extracted from receipt
/// </summary>
public class OcrLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
}
