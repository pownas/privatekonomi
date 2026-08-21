using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.ML;

/// <summary>
/// Service for ML-based transaction categorization using ML.NET.
/// </summary>
public class TransactionMLService : ITransactionMLService
{
    private readonly PrivatekonomyContext _context;
    private readonly ILogger<TransactionMLService> _logger;
    private readonly MLContext _mlContext;
    private readonly Dictionary<string, ITransformer> _loadedModels;
    private readonly string _modelStoragePath;
    
    // Minimum requirements for training
    private const int MinimumTransactionsRequired = 50;
    private const int MinimumExamplesPerCategory = 5;
    private const float ConfidenceThreshold = 0.7f;

    public TransactionMLService(
        PrivatekonomyContext context,
        ILogger<TransactionMLService> logger,
        string? modelStoragePath = null)
    {
        _context = context;
        _logger = logger;
        _mlContext = new MLContext(seed: 0);
        _loadedModels = new Dictionary<string, ITransformer>();
        _modelStoragePath = modelStoragePath ?? Path.Combine(Directory.GetCurrentDirectory(), "ML_Models");
        
        // Ensure model storage directory exists
        Directory.CreateDirectory(_modelStoragePath);
    }

    public async Task<ModelMetrics?> TrainModelAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Starting model training for user {UserId}", userId);
            
            // Get all categorized transactions for the user
            var transactions = await GetTrainingDataAsync(userId);
            
            if (transactions.Count < MinimumTransactionsRequired)
            {
                _logger.LogWarning(
                    "Insufficient training data for user {UserId}. Found {Count} transactions, need at least {Required}",
                    userId, transactions.Count, MinimumTransactionsRequired);
                return null;
            }
            
            // Validate minimum examples per category
            var categoryGroups = transactions.GroupBy(t => t.Category);
            var insufficientCategories = categoryGroups
                .Where(g => g.Count() < MinimumExamplesPerCategory)
                .Select(g => g.Key)
                .ToList();
            
            if (insufficientCategories.Any())
            {
                _logger.LogWarning(
                    "Some categories have insufficient examples for user {UserId}: {Categories}",
                    userId, string.Join(", ", insufficientCategories));
            }
            
            // Filter out categories with too few examples
            transactions = transactions
                .GroupBy(t => t.Category)
                .Where(g => g.Count() >= MinimumExamplesPerCategory)
                .SelectMany(g => g)
                .ToList();
            
            if (transactions.Count < MinimumTransactionsRequired)
            {
                _logger.LogWarning(
                    "After filtering insufficient categories, not enough data remains for user {UserId}",
                    userId);
                return null;
            }
            
            // Create data view
            var dataView = _mlContext.Data.LoadFromEnumerable(transactions);
            
            // Split data for training and testing (80/20)
            var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            
            // Build training pipeline
            var pipeline = BuildTrainingPipeline();
            
            // Train the model
            _logger.LogInformation("Training model for user {UserId} with {Count} transactions", userId, transactions.Count);
            var model = pipeline.Fit(trainTestSplit.TrainSet);
            
            // Evaluate the model
            var predictions = model.Transform(trainTestSplit.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");
            
            // Save the model
            var modelPath = GetModelPath(userId);
            await SaveModelToDiskAsync(model, dataView.Schema, modelPath);
            
            // Store model in cache
            _loadedModels[userId] = model;
            
            // Save model metadata to database
            await SaveModelMetadataAsync(userId, modelPath, transactions.Count, metrics);
            
            var modelMetrics = new ModelMetrics
            {
                Accuracy = metrics.MicroAccuracy,
                MacroPrecision = metrics.MacroAccuracy, // Note: ML.NET doesn't provide separate MacroPrecision for multiclass
                MacroRecall = metrics.MacroAccuracy, // Note: ML.NET doesn't provide separate MacroRecall for multiclass
                MicroAccuracy = metrics.MicroAccuracy,
                LogLoss = metrics.LogLoss,
                LogLossReduction = metrics.LogLossReduction
            };
            
            _logger.LogInformation(
                "Model training completed for user {UserId}. Accuracy: {Accuracy:P2}",
                userId, modelMetrics.Accuracy);
            
            return modelMetrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model for user {UserId}", userId);
            return null;
        }
    }

    public async Task<ModelMetrics?> EvaluateModelAsync(string userId)
    {
        try
        {
            var model = await GetOrLoadModelAsync(userId);
            if (model == null)
            {
                _logger.LogWarning("No trained model found for user {UserId}", userId);
                return null;
            }
            
            var transactions = await GetTrainingDataAsync(userId);
            if (transactions.Count < MinimumTransactionsRequired)
            {
                return null;
            }
            
            var dataView = _mlContext.Data.LoadFromEnumerable(transactions);
            var predictions = model.Transform(dataView);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");
            
            return new ModelMetrics
            {
                Accuracy = metrics.MicroAccuracy,
                MacroPrecision = metrics.MacroAccuracy, // Note: ML.NET doesn't provide separate MacroPrecision for multiclass
                MacroRecall = metrics.MacroAccuracy, // Note: ML.NET doesn't provide separate MacroRecall for multiclass
                MicroAccuracy = metrics.MicroAccuracy,
                LogLoss = metrics.LogLoss,
                LogLossReduction = metrics.LogLossReduction
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model for user {UserId}", userId);
            return null;
        }
    }

    public async Task<CategoryPrediction?> PredictCategoryAsync(Transaction transaction, string userId)
    {
        try
        {
            var model = await GetOrLoadModelAsync(userId);
            if (model == null)
            {
                _logger.LogDebug("No trained model found for user {UserId}, skipping ML prediction", userId);
                return null;
            }
            
            var features = ExtractFeatures(transaction);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<TransactionFeatures, CategoryPredictionInternal>(model);
            var prediction = predictionEngine.Predict(features);
            
            // Get top 3 alternative categories
            var alternatives = new Dictionary<string, float>();
            if (prediction.Score != null && prediction.Score.Length > 0)
            {
                var topScores = prediction.Score
                    .Select((score, index) => new { Score = score, Index = index })
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .ToList();
                
                // We'd need category names here, but for simplicity we'll use the predicted category
                // In a real implementation, you'd want to maintain a mapping of indices to category names
            }
            
            return new CategoryPrediction
            {
                Category = prediction.PredictedCategory,
                Confidence = prediction.Score?.Max() ?? 0f,
                AlternativeCategories = alternatives
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting category for transaction {TransactionId}", transaction.TransactionId);
            return null;
        }
    }

    public async Task<List<CategoryPrediction>> PredictBatchAsync(List<Transaction> transactions, string userId)
    {
        var predictions = new List<CategoryPrediction>();
        
        foreach (var transaction in transactions)
        {
            var prediction = await PredictCategoryAsync(transaction, userId);
            if (prediction != null)
            {
                predictions.Add(prediction);
            }
        }
        
        return predictions;
    }

    public async Task SaveModelAsync(string userId, string modelPath)
    {
        if (_loadedModels.TryGetValue(userId, out var model))
        {
            var transactions = await GetTrainingDataAsync(userId);
            var dataView = _mlContext.Data.LoadFromEnumerable(transactions);
            await SaveModelToDiskAsync(model, dataView.Schema, modelPath);
        }
    }

    public async Task LoadModelAsync(string userId)
    {
        var modelPath = GetModelPath(userId);
        if (File.Exists(modelPath))
        {
            var model = _mlContext.Model.Load(modelPath, out var _);
            _loadedModels[userId] = model;
            _logger.LogInformation("Model loaded for user {UserId} from {Path}", userId, modelPath);
        }
        else
        {
            _logger.LogWarning("Model file not found for user {UserId} at {Path}", userId, modelPath);
        }
        
        await Task.CompletedTask;
    }

    public async Task<bool> IsModelTrainedAsync(string userId)
    {
        // Check if model is in cache
        if (_loadedModels.ContainsKey(userId))
        {
            return true;
        }
        
        // Check if model exists in database
        var modelMetadata = await _context.MLModels
            .FirstOrDefaultAsync(m => m.UserId == userId);
        
        return modelMetadata != null && File.Exists(modelMetadata.ModelPath);
    }

    public async Task UpdateModelWithFeedbackAsync(
        string userId, 
        Transaction transaction, 
        string correctCategory, 
        string predictedCategory, 
        float confidence)
    {
        try
        {
            var feedback = new UserFeedback
            {
                UserId = userId,
                TransactionId = transaction.TransactionId,
                PredictedCategory = predictedCategory,
                PredictedConfidence = confidence,
                ActualCategory = correctCategory,
                WasCorrectionNeeded = predictedCategory != correctCategory,
                FeedbackDate = DateTime.UtcNow
            };
            
            _context.UserFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Feedback recorded for user {UserId}, transaction {TransactionId}. Predicted: {Predicted}, Actual: {Actual}",
                userId, transaction.TransactionId, predictedCategory, correctCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording feedback for user {UserId}", userId);
        }
    }

    // Private helper methods
    
    private async Task<List<TransactionFeatures>> GetTrainingDataAsync(string userId)
    {
        var transactions = await _context.Transactions
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => t.UserId == userId && t.TransactionCategories.Any())
            .ToListAsync();
        
        var features = new List<TransactionFeatures>();
        
        foreach (var transaction in transactions)
        {
            // Get the primary category (first one if multiple)
            var category = transaction.TransactionCategories.FirstOrDefault()?.Category;
            if (category == null) continue;
            
            var feature = ExtractFeatures(transaction);
            feature.Category = category.Name;
            features.Add(feature);
        }
        
        return features;
    }

    private TransactionFeatures ExtractFeatures(Transaction transaction)
    {
        var amount = (float)transaction.Amount;
        var amountLog = amount > 0 ? (float)Math.Log10((double)amount) : 0f;
        var dayOfWeek = (float)transaction.Date.DayOfWeek;
        var dayOfMonth = (float)transaction.Date.Day;
        var isWeekend = transaction.Date.DayOfWeek == DayOfWeek.Saturday || 
                        transaction.Date.DayOfWeek == DayOfWeek.Sunday;
        var isMonthStart = transaction.Date.Day <= 5;
        var isMonthEnd = transaction.Date.Day >= DateTime.DaysInMonth(transaction.Date.Year, transaction.Date.Month) - 5;
        
        return new TransactionFeatures
        {
            Description = transaction.Description ?? string.Empty,
            Payee = transaction.Payee ?? string.Empty,
            Amount = amount,
            AmountLog = amountLog,
            DayOfWeek = dayOfWeek,
            DayOfMonth = dayOfMonth,
            IsWeekend = isWeekend,
            IsMonthStart = isMonthStart,
            IsMonthEnd = isMonthEnd
        };
    }

    private Microsoft.ML.Data.EstimatorChain<Microsoft.ML.Transforms.KeyToValueMappingTransformer> BuildTrainingPipeline()
    {
        // Convert boolean features to float
        var pipeline = _mlContext.Transforms.Conversion.ConvertType(
                new[] {
                    new InputOutputColumnPair("IsWeekendFloat", nameof(TransactionFeatures.IsWeekend)),
                    new InputOutputColumnPair("IsMonthStartFloat", nameof(TransactionFeatures.IsMonthStart)),
                    new InputOutputColumnPair("IsMonthEndFloat", nameof(TransactionFeatures.IsMonthEnd))
                },
                DataKind.Single)
            // Text featurization - using bag of words
            .Append(_mlContext.Transforms.Text.NormalizeText(
                "NormalizedDescription",
                nameof(TransactionFeatures.Description)))
            .Append(_mlContext.Transforms.Text.TokenizeIntoWords(
                "TokenizedDescription",
                "NormalizedDescription"))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                "TokenizedDescriptionKeys",
                "TokenizedDescription"))
            .Append(_mlContext.Transforms.Text.ProduceNgrams(
                "DescriptionFeaturized",
                "TokenizedDescriptionKeys"))
            // Concatenate all features
            .Append(_mlContext.Transforms.Concatenate(
                "Features",
                "DescriptionFeaturized",
                nameof(TransactionFeatures.Amount),
                nameof(TransactionFeatures.AmountLog),
                nameof(TransactionFeatures.DayOfWeek),
                nameof(TransactionFeatures.DayOfMonth),
                "IsWeekendFloat",
                "IsMonthStartFloat",
                "IsMonthEndFloat"))
            // Convert category to key for training
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: nameof(TransactionFeatures.Category)))
            // Train the model
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            // Convert predicted label back to category name
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                outputColumnName: nameof(CategoryPredictionInternal.PredictedCategory),
                inputColumnName: "PredictedLabel"));
        
        return pipeline;
    }

    private async Task SaveModelToDiskAsync(ITransformer model, DataViewSchema schema, string modelPath)
    {
        using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.Write);
        _mlContext.Model.Save(model, schema, fileStream);
        await Task.CompletedTask;
    }

    private async Task SaveModelMetadataAsync(
        string userId, 
        string modelPath, 
        int trainingRecordsCount, 
        MulticlassClassificationMetrics metrics)
    {
        // Remove existing model metadata for this user
        var existing = await _context.MLModels
            .FirstOrDefaultAsync(m => m.UserId == userId);
        
        if (existing != null)
        {
            _context.MLModels.Remove(existing);
        }
        
        var modelMetadata = new Models.MLModel
        {
            UserId = userId,
            ModelPath = modelPath,
            TrainedAt = DateTime.UtcNow,
            TrainingRecordsCount = trainingRecordsCount,
            Accuracy = (float)metrics.MicroAccuracy,
            Precision = (float)metrics.MacroAccuracy, // Note: ML.NET doesn't provide separate precision for multiclass
            Recall = (float)metrics.MacroAccuracy, // Note: ML.NET doesn't provide separate recall for multiclass
            Metrics = System.Text.Json.JsonSerializer.Serialize(new
            {
                MicroAccuracy = metrics.MicroAccuracy,
                MacroAccuracy = metrics.MacroAccuracy,
                LogLoss = metrics.LogLoss,
                LogLossReduction = metrics.LogLossReduction
            })
        };
        
        _context.MLModels.Add(modelMetadata);
        await _context.SaveChangesAsync();
    }

    private async Task<ITransformer?> GetOrLoadModelAsync(string userId)
    {
        // Check cache first
        if (_loadedModels.TryGetValue(userId, out var model))
        {
            return model;
        }
        
        // Try to load from disk
        var modelPath = GetModelPath(userId);
        if (File.Exists(modelPath))
        {
            await LoadModelAsync(userId);
            return _loadedModels.TryGetValue(userId, out var loadedModel) ? loadedModel : null;
        }
        
        return null;
    }

    private string GetModelPath(string userId)
    {
        return Path.Combine(_modelStoragePath, $"transaction_model_{userId}.zip");
    }

    // Internal class for ML.NET prediction
    private sealed class CategoryPredictionInternal
    {
        public string PredictedCategory { get; set; } = string.Empty;
        
        public float[]? Score { get; set; }
    }
}
