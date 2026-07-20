using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;

namespace Privatekonomi.Core.Services;

/// <summary>
/// OCR service implementation using Tesseract OCR engine
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _tessDataPath;
    private const string Language = "swe+eng"; // Swedish + English for better recognition

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
        // Tesseract data path - will be configured based on environment
        _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                // Check if tessdata directory exists
                if (!Directory.Exists(_tessDataPath))
                {
                    _logger.LogWarning("Tesseract data directory not found at: {Path}", _tessDataPath);
                    return false;
                }

                // Check for language data files
                var swedishDataFile = Path.Combine(_tessDataPath, "swe.traineddata");
                var englishDataFile = Path.Combine(_tessDataPath, "eng.traineddata");
                
                if (!File.Exists(englishDataFile))
                {
                    _logger.LogWarning("English language data file not found at: {Path}", englishDataFile);
                    return false;
                }

                _logger.LogInformation("OCR service is available");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking OCR availability");
                return false;
            }
        });
    }

    public async Task<OcrResult> ProcessReceiptImageAsync(Stream imageStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Processing receipt image: {FileName}", fileName);

            // Preprocess the image for better OCR accuracy
            using var preprocessedImage = await PreprocessImageAsync(imageStream);
            
            // Convert to byte array for Tesseract
            using var ms = new MemoryStream();
            await preprocessedImage.SaveAsPngAsync(ms);
            var imageBytes = ms.ToArray();

            // Perform OCR
            var ocrText = await PerformOcrAsync(imageBytes);
            
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("No text extracted from image");
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "Ingen text kunde läsas från bilden. Försök med en tydligare bild."
                };
            }

            _logger.LogInformation("OCR extracted {Length} characters", ocrText.Length);

            // Parse the extracted text
            var parsedData = ParseReceiptText(ocrText);

            return new OcrResult
            {
                Success = true,
                RawText = ocrText,
                ParsedData = parsedData,
                Confidence = CalculateConfidence(parsedData)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt image");
            return new OcrResult
            {
                Success = false,
                ErrorMessage = $"Ett fel uppstod vid bildbehandling: {ex.Message}"
            };
        }
    }

    private async Task<Image<Rgb24>> PreprocessImageAsync(Stream imageStream)
    {
        // Load the image
        var image = await Image.LoadAsync<Rgb24>(imageStream);

        // Apply preprocessing to improve OCR accuracy
        image.Mutate(x => x
            .Grayscale()                    // Convert to grayscale
            .Contrast(1.2f)                 // Increase contrast
            .GaussianSharpen()              // Sharpen the image
        );

        // Optionally resize if image is too large or too small
        if (image.Width > 2000 || image.Height > 2000)
        {
            var ratio = Math.Min(2000.0 / image.Width, 2000.0 / image.Height);
            image.Mutate(x => x.Resize((int)(image.Width * ratio), (int)(image.Height * ratio)));
        }
        else if (image.Width < 800 && image.Height < 800)
        {
            var ratio = Math.Max(800.0 / image.Width, 800.0 / image.Height);
            image.Mutate(x => x.Resize((int)(image.Width * ratio), (int)(image.Height * ratio)));
        }

        return image;
    }

    private async Task<string> PerformOcrAsync(byte[] imageBytes)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var engine = new TesseractEngine(_tessDataPath, Language, EngineMode.Default);
                using var img = Pix.LoadFromMemory(imageBytes);
                using var page = engine.Process(img);
                
                var text = page.GetText();
                var confidence = page.GetMeanConfidence();
                
                _logger.LogInformation("OCR completed with confidence: {Confidence:P}", confidence);
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Tesseract OCR processing");
                throw;
            }
        });
    }

    public OcrReceiptData ParseReceiptText(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return new OcrReceiptData();
        }

        var data = new OcrReceiptData();
        var lines = ocrText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        // Extract total amount
        data.TotalAmount = ExtractTotalAmount(lines);

        // Extract date
        data.Date = ExtractDate(lines);

        // Extract merchant (usually in the first few lines)
        data.Merchant = ExtractMerchant(lines);

        // Extract receipt number
        data.ReceiptNumber = ExtractReceiptNumber(lines);

        // Extract payment method
        data.PaymentMethod = ExtractPaymentMethod(lines);

        // Extract line items
        data.LineItems = ExtractLineItems(lines);

        return data;
    }

    private decimal? ExtractTotalAmount(List<string> lines)
    {
        // Patterns for Swedish receipts - prioritize patterns with keywords
        var patterns = new[]
        {
            @"(?i)(?:totalt|total|att\s+betala|summa|belopp)[:\s]+(?:kr|sek)?\s*([0-9]{1,6}[,\.]?[0-9]{0,2})",
            @"(?i)(?:kr|sek)\s*([0-9]{1,6}[,\.]?[0-9]{0,2})\s*(?:totalt|total)"
        };

        // First pass: look for lines with total keywords
        foreach (var line in lines)
        {
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    var amountStr = match.Groups[1].Value.Replace(",", ".").Replace(" ", "");
                    if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                    {
                        if (amount > 0 && amount < 1000000) // Reasonable receipt amount
                        {
                            return amount;
                        }
                    }
                }
            }
        }

        // Second pass: look for amounts at the end of lines (less reliable)
        // Only use this if we haven't found a total with keywords
        var endOfLinePattern = @"([0-9]{1,6}[,\.][0-9]{2})\s*(?:kr|sek)?\s*$";
        var candidates = new List<decimal>();
        
        foreach (var line in lines)
        {
            // Skip lines that look like item lines (have text before the amount)
            if (Regex.IsMatch(line, @"^.{10,}\s+[0-9]{1,6}[,\.][0-9]{2}"))
                continue;
                
            var match = Regex.Match(line, endOfLinePattern);
            if (match.Success)
            {
                var amountStr = match.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    if (amount > 0 && amount < 1000000)
                    {
                        candidates.Add(amount);
                    }
                }
            }
        }

        // Return the highest amount found (likely to be the total)
        return candidates.Any() ? candidates.Max() : null;
    }

    private DateTime? ExtractDate(List<string> lines)
    {
        // Swedish date patterns
        var patterns = new[]
        {
            @"(\d{4}-\d{2}-\d{2})",                     // 2024-01-15
            @"(\d{2}/\d{2}/\d{4})",                     // 15/01/2024
            @"(\d{2}\.\d{2}\.\d{4})",                   // 15.01.2024
            @"(\d{2}-\d{2}-\d{4})",                     // 15-01-2024
            @"(\d{4}\d{2}\d{2})",                       // 20240115
            @"(?i)(datum|date)[:\s]+(\d{2,4}[-/.]\d{2}[-/.]\d{2,4})"
        };

        foreach (var line in lines.Take(10)) // Check first 10 lines
        {
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    var dateStr = match.Groups[match.Groups.Count > 2 ? 2 : 1].Value;
                    
                    // Try different date formats
                    var formats = new[] 
                    { 
                        "yyyy-MM-dd", "dd/MM/yyyy", "dd.MM.yyyy", 
                        "dd-MM-yyyy", "yyyyMMdd", "yyyy/MM/dd" 
                    };

                    foreach (var format in formats)
                    {
                        if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, 
                            DateTimeStyles.None, out var date))
                        {
                            // Sanity check - date should be within reasonable range
                            if (date.Year >= 2000 && date <= DateTime.Now.AddDays(1))
                            {
                                return date;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private string? ExtractMerchant(List<string> lines)
    {
        // Merchant is typically in the first few lines
        // Look for the longest line in the first 5 lines that's not too long
        var candidates = lines.Take(5)
            .Where(l => l.Length > 3 && l.Length < 50)
            .Where(l => !Regex.IsMatch(l, @"^\d+$")) // Not just numbers
            .Where(l => !Regex.IsMatch(l, @"(?i)(datum|kvitto|receipt|bon)")) // Not common header words
            .OrderByDescending(l => l.Length)
            .FirstOrDefault();

        return candidates;
    }

    private string? ExtractReceiptNumber(List<string> lines)
    {
        var patterns = new[]
        {
            @"(?i)(?:kvitto|receipt|bon)[:\s#-]*(\d+)",
            @"(?i)(?:nr|number)[:\s#-]*(\d+)",
            @"#(\d{4,})"
        };

        foreach (var line in lines.Take(15))
        {
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
        }

        return null;
    }

    private string? ExtractPaymentMethod(List<string> lines)
    {
        var paymentMethods = new Dictionary<string, string>
        {
            { @"(?i)kort|card|kreditkort|bankkort", "Kort" },
            { @"(?i)swish", "Swish" },
            { @"(?i)kontant|cash", "Kontant" },
            { @"(?i)autogiro", "Autogiro" },
            { @"(?i)e-?faktura", "E-faktura" }
        };

        foreach (var line in lines)
        {
            foreach (var (pattern, method) in paymentMethods)
            {
                if (Regex.IsMatch(line, pattern))
                {
                    return method;
                }
            }
        }

        return null;
    }

    private List<OcrLineItem> ExtractLineItems(List<string> lines)
    {
        var items = new List<OcrLineItem>();

        // Pattern for line items: description ... price
        // Example: "Mjölk 1,5%        29.50" or "2x Bröd        45,00"
        var pattern = @"^(.+?)\s+([0-9]{1,6}[,\.][0-9]{2})\s*(?:kr|sek)?$";

        foreach (var line in lines)
        {
            var match = Regex.Match(line, pattern);
            if (match.Success && match.Groups.Count > 2)
            {
                var description = match.Groups[1].Value.Trim();
                var priceStr = match.Groups[2].Value.Replace(",", ".");

                if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    // Check if description starts with quantity like "2x" or "3 st"
                    var qtyMatch = Regex.Match(description, @"^(\d+)\s*(?:x|st)\s+(.+)");
                    if (qtyMatch.Success)
                    {
                        var qty = int.Parse(qtyMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        description = qtyMatch.Groups[2].Value.Trim();
                        
                        items.Add(new OcrLineItem
                        {
                            Description = description,
                            Quantity = qty,
                            UnitPrice = price / qty,
                            TotalPrice = price
                        });
                    }
                    else
                    {
                        items.Add(new OcrLineItem
                        {
                            Description = description,
                            Quantity = 1,
                            TotalPrice = price,
                            UnitPrice = price
                        });
                    }
                }
            }
        }

        return items;
    }

    private float CalculateConfidence(OcrReceiptData data)
    {
        var confidence = 0f;
        var factors = 0;

        if (!string.IsNullOrEmpty(data.Merchant)) { confidence += 0.25f; factors++; }
        if (data.TotalAmount.HasValue) { confidence += 0.35f; factors++; }
        if (data.Date.HasValue) { confidence += 0.25f; factors++; }
        if (data.LineItems.Any()) { confidence += 0.15f; factors++; }

        return factors > 0 ? confidence : 0f;
    }
}
