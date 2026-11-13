// Author: Damianos Pythagoras Ougkrenidis
// ML - Semantic Search & NLP (Vector DB, Embeddings, Natural Language Search)

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace INCIS_SaaP.ML.SemanticSearch;

/// <summary>
/// Embedding vector stored for semantic search
/// </summary>
public class EmbeddingRecord
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Entity type: "Formula", "RawMaterial", "INCI"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;
    
    public int EntityId { get; set; }
    
    /// <summary>
    /// Original text that was embedded
    /// </summary>
    [Required]
    public string OriginalText { get; set; } = string.Empty;
    
    /// <summary>
    /// Embedding vector (JSON array of floats)
    /// In production, would use pgvector or dedicated vector DB
    /// </summary>
    [Required]
    public string VectorJson { get; set; } = "[]";
    
    /// <summary>
    /// Embedding model used
    /// </summary>
    [MaxLength(100)]
    public string ModelName { get; set; } = "local-sentence-transformer";
    
    /// <summary>
    /// Dimension of the embedding vector
    /// </summary>
    public int Dimension { get; set; } = 384;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Search result from semantic search
/// </summary>
public class SemanticSearchResult
{
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double SimilarityScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Auto-tagging result
/// </summary>
public class AutoTagResult
{
    public int EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public List<TagPrediction> PredictedTags { get; init; } = new();
}

public class TagPrediction
{
    public string Tag { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Category { get; init; } = string.Empty;
}

/// <summary>
/// Semantic Search Service interface
/// </summary>
public interface ISemanticSearchService
{
    /// <summary>
    /// Searches using natural language query
    /// </summary>
    Task<List<SemanticSearchResult>> SearchAsync(string query, string? entityType = null, int maxResults = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indexes an entity for semantic search
    /// </summary>
    Task IndexEntityAsync(string entityType, int entityId, string textContent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds similar entities
    /// </summary>
    Task<List<SemanticSearchResult>> FindSimilarAsync(string entityType, int entityId, int maxResults = 5, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Auto-tags a raw material based on its INCI composition
    /// </summary>
    Task<AutoTagResult> AutoTagRawMaterialAsync(int rawMaterialId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rebuilds the search index (called by background job)
    /// </summary>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Semantic Search Service implementation
/// Uses TF-IDF + cosine similarity (can be upgraded to OpenAI/Ollama embeddings)
/// </summary>
public class SemanticSearchService : ISemanticSearchService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SemanticSearchService> _logger;

    // Category classification based on INCI keywords
    private static readonly Dictionary<string, List<string>> CategoryKeywords = new()
    {
        ["Preservatives"] = new() { "PHENOXYETHANOL", "BENZOIC ACID", "SORBIC ACID", "BENZISOTHIAZOLINONE", "METHYLISOTHIAZOLINONE" },
        ["UV Filters"] = new() { "ETHYLHEXYL METHOXYCINNAMATE", "OCTOCRYLENE", "TITANIUM DIOXIDE", "ZINC OXIDE", "BEMOTRIZINOL" },
        ["Emollients"] = new() { "DIMETHICONE", "ISOPROPYL MYRISTATE", "CAPRYLIC/CAPRIC TRIGLYCERIDE", "COCO-CAPRYLATE", "SQUALANE" },
        ["Surfactants"] = new() { "SODIUM LAURETH SULFATE", "COCAMIDOPROPYL BETAINE", "DECYL GLUCOSIDE", "COCO-GLUCOSIDE" },
        ["Emulsifiers"] = new() { "CETEARYL ALCOHOL", "GLYCERYL STEARATE", "POLYSORBATE 60", "PEG-100 STEARATE" },
        ["Humectants"] = new() { "GLYCERIN", "HYALURONIC ACID", "PANTHENOL", "PROPYLENE GLYCOL", "BETAINE" },
        ["Thickeners"] = new() { "CARBOMER", "XANTHAN GUM", "HYDROXYETHYLCELLULOSE", "ACRYLATES" },
        ["Antioxidants"] = new() { "TOCOPHEROL", "ASCORBIC ACID", "BHT", "RESVERATROL" },
        ["Colorants"] = new() { "CI 77891", "CI 77491", "CI 77492", "CI 77499" },
        ["Fragrances"] = new() { "PARFUM", "FRAGRANCE", "AROMA" }
    };

    public SemanticSearchService(IServiceProvider serviceProvider, ILogger<SemanticSearchService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<SemanticSearchResult>> SearchAsync(string query, string? entityType = null, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SemanticSearchResult>();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Get all embeddings (in production, would use vector DB)
        var embeddingsQuery = dbContext.Set<EmbeddingRecord>().AsQueryable();
        if (!string.IsNullOrEmpty(entityType))
            embeddingsQuery = embeddingsQuery.Where(e => e.EntityType == entityType);

        var embeddings = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(embeddingsQuery, cancellationToken);

        // Compute query vector (simplified TF-IDF approach)
        var queryTerms = TokenizeAndNormalize(query);

        // Score each document
        var scored = embeddings.Select(e =>
        {
            var docTerms = TokenizeAndNormalize(e.OriginalText);
            var score = ComputeCosineSimilarity(queryTerms, docTerms);
            return new { Embedding = e, Score = score };
        })
        .Where(x => x.Score > 0.1)
        .OrderByDescending(x => x.Score)
        .Take(maxResults)
        .ToList();

        return scored.Select(s => new SemanticSearchResult
        {
            EntityType = s.Embedding.EntityType,
            EntityId = s.Embedding.EntityId,
            Title = s.Embedding.OriginalText.Length > 100 ? s.Embedding.OriginalText[..100] + "..." : s.Embedding.OriginalText,
            Description = s.Embedding.OriginalText,
            SimilarityScore = s.Score
        }).ToList();
    }

    public async Task IndexEntityAsync(string entityType, int entityId, string textContent, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Remove existing embedding
        var existing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<EmbeddingRecord>()
                    .Where(e => e.EntityType == entityType && e.EntityId == entityId),
                cancellationToken);

        if (existing != null)
        {
            existing.OriginalText = textContent;
            existing.VectorJson = JsonSerializer.Serialize(ComputeSimpleVector(textContent));
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            dbContext.Set<EmbeddingRecord>().Add(new EmbeddingRecord
            {
                EntityType = entityType,
                EntityId = entityId,
                OriginalText = textContent,
                VectorJson = JsonSerializer.Serialize(ComputeSimpleVector(textContent)),
                Dimension = 100
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SemanticSearchResult>> FindSimilarAsync(string entityType, int entityId, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var source = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<EmbeddingRecord>()
                    .Where(e => e.EntityType == entityType && e.EntityId == entityId),
                cancellationToken);

        if (source == null) return new List<SemanticSearchResult>();

        return await SearchAsync(source.OriginalText, entityType, maxResults, cancellationToken);
    }

    public async Task<AutoTagResult> AutoTagRawMaterialAsync(int rawMaterialId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var rm = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<Models.RawMaterial>()
                    .Include(r => r.Composition)
                    .ThenInclude(c => c.INCI)
                    .Where(r => r.Id == rawMaterialId),
                cancellationToken);

        if (rm == null)
            return new AutoTagResult { EntityId = rawMaterialId, EntityType = "RawMaterial" };

        var tags = new List<TagPrediction>();

        // Classify based on INCI composition
        var inciNames = rm.Composition
            .Where(c => c.INCI != null && !c.IsImpurity)
            .Select(c => c.INCI!.Name.ToUpperInvariant())
            .ToList();

        foreach (var (category, keywords) in CategoryKeywords)
        {
            var matchCount = inciNames.Count(name => keywords.Any(kw => name.Contains(kw)));
            if (matchCount > 0)
            {
                var confidence = Math.Min(1.0, matchCount * 0.3 + 0.2);
                tags.Add(new TagPrediction
                {
                    Tag = category,
                    Confidence = confidence,
                    Category = "ProductCategory"
                });
            }
        }

        // Add function-based tags
        if (rm.NaturalOriginPercentage >= 95)
            tags.Add(new TagPrediction { Tag = "Natural", Confidence = 0.95, Category = "Origin" });
        else if (rm.NaturalOriginPercentage >= 70)
            tags.Add(new TagPrediction { Tag = "Natural-Derived", Confidence = 0.85, Category = "Origin" });

        if (rm.HasVegan)
            tags.Add(new TagPrediction { Tag = "Vegan", Confidence = 1.0, Category = "Certification" });

        return new AutoTagResult
        {
            EntityId = rawMaterialId,
            EntityType = "RawMaterial",
            PredictedTags = tags.OrderByDescending(t => t.Confidence).ToList()
        };
    }

    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        _logger.LogInformation("Rebuilding semantic search index...");

        // Index formulas
        var formulas = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(dbContext.Set<Models.Formula>(), cancellationToken);
        
        foreach (var f in formulas)
        {
            var text = $"{f.FormulaCode} {f.ProductName} {f.FormulaName} {f.ProductType} {f.Comments}";
            await IndexEntityAsync("Formula", f.Id, text, cancellationToken);
        }

        // Index raw materials
        var rawMaterials = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(dbContext.Set<Models.RawMaterial>(), cancellationToken);
        
        foreach (var rm in rawMaterials)
        {
            var text = $"{rm.ProductName} {rm.ManufacturerName} {rm.Category} {rm.Comments} {rm.Allergens}";
            await IndexEntityAsync("RawMaterial", rm.Id, text, cancellationToken);
        }

        _logger.LogInformation("Semantic search index rebuilt: {FormulaCount} formulas, {RMCount} raw materials", 
            formulas.Count, rawMaterials.Count);
    }

    // Simplified text processing (in production would use proper NLP)
    private static Dictionary<string, double> TokenizeAndNormalize(string text)
    {
        var tokens = text.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '-', '/', '(', ')', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => (double)g.Count());

        // TF normalization
        var maxFreq = tokens.Values.Any() ? tokens.Values.Max() : 1;
        return tokens.ToDictionary(kv => kv.Key, kv => kv.Value / maxFreq);
    }

    private static double ComputeCosineSimilarity(Dictionary<string, double> vec1, Dictionary<string, double> vec2)
    {
        var allTerms = vec1.Keys.Union(vec2.Keys);
        double dotProduct = 0, norm1 = 0, norm2 = 0;

        foreach (var term in allTerms)
        {
            var v1 = vec1.GetValueOrDefault(term);
            var v2 = vec2.GetValueOrDefault(term);
            dotProduct += v1 * v2;
            norm1 += v1 * v1;
            norm2 += v2 * v2;
        }

        if (norm1 == 0 || norm2 == 0) return 0;
        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    private static float[] ComputeSimpleVector(string text)
    {
        // Simplified vector (in production would use sentence-transformers)
        var tokens = TokenizeAndNormalize(text);
        var vector = new float[100];
        var i = 0;
        foreach (var (term, weight) in tokens.Take(100))
        {
            vector[i % 100] += (float)(term.GetHashCode() % 1000 / 1000.0 * weight);
            i++;
        }
        return vector;
    }
}

