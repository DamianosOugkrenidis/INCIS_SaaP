// Author: Damianos Pythagoras Ougkrenidis
// Product Categories enum for Raw Materials

namespace INCIS_SaaP.Models;

/// <summary>
/// Κατηγορίες προϊόντων για α' ύλες
/// Based on cosmetic ingredient functionality
/// </summary>
public enum ProductCategory
{
    // Preservatives
    Preservatives,
    
    // UV Filters & Sun Protection
    UVFilters,
    
    // Emollients & Moisturizers
    Emollients,
    
    // Emulsifiers & Stabilizers
    Emulsifiers,
    
    // Thickeners & Viscosity Modifiers
    Thickeners,
    
    // Surfactants & Cleansing Agents
    Surfactants,
    
    // Active Ingredients
    ActiveIngredients,
    
    // Fragrances & Essential Oils
    Fragrances,
    
    // Colorants & Pigments
    Colorants,
    
    // Humectants
    Humectants,
    
    // Antioxidants
    Antioxidants,
    
    // Chelating Agents
    ChelatingAgents,
    
    // pH Adjusters
    PHAdjusters,
    
    // Solvents
    Solvents,
    
    // Film Formers
    FilmFormers,
    
    // Exfoliants
    Exfoliants,
    
    // Botanicals & Extracts
    Botanicals,
    
    // Proteins & Peptides
    Proteins,
    
    // Vitamins
    Vitamins,
    
    // Minerals
    Minerals,
    
    // Waxes
    Waxes,
    
    // Oils & Butters
    OilsAndButters,
    
    // Silicones
    Silicones,
    
    // Polymers
    Polymers,
    
    // Anti-aging
    AntiAging,
    
    // Skin Brightening
    SkinBrightening,
    
    // Acne Treatment
    AcneTreatment,
    
    // Other
    Other
}

/// <summary>
/// Helper class για Product Categories
/// </summary>
public static class ProductCategoryHelper
{
    /// <summary>
    /// Get display name for category
    /// </summary>
    public static string GetDisplayName(ProductCategory category)
    {
        return category switch
        {
            ProductCategory.Preservatives => "Preservatives",
            ProductCategory.UVFilters => "UV Filters",
            ProductCategory.Emollients => "Emollients",
            ProductCategory.Emulsifiers => "Emulsifiers",
            ProductCategory.Thickeners => "Thickeners",
            ProductCategory.Surfactants => "Surfactants",
            ProductCategory.ActiveIngredients => "Active Ingredients",
            ProductCategory.Fragrances => "Fragrances",
            ProductCategory.Colorants => "Colorants",
            ProductCategory.Humectants => "Humectants",
            ProductCategory.Antioxidants => "Antioxidants",
            ProductCategory.ChelatingAgents => "Chelating Agents",
            ProductCategory.PHAdjusters => "pH Adjusters",
            ProductCategory.Solvents => "Solvents",
            ProductCategory.FilmFormers => "Film Formers",
            ProductCategory.Exfoliants => "Exfoliants",
            ProductCategory.Botanicals => "Botanicals & Extracts",
            ProductCategory.Proteins => "Proteins & Peptides",
            ProductCategory.Vitamins => "Vitamins",
            ProductCategory.Minerals => "Minerals",
            ProductCategory.Waxes => "Waxes",
            ProductCategory.OilsAndButters => "Oils & Butters",
            ProductCategory.Silicones => "Silicones",
            ProductCategory.Polymers => "Polymers",
            ProductCategory.AntiAging => "Anti-aging",
            ProductCategory.SkinBrightening => "Skin Brightening",
            ProductCategory.AcneTreatment => "Acne Treatment",
            ProductCategory.Other => "Other",
            _ => category.ToString()
        };
    }

    /// <summary>
    /// Get all categories as list
    /// </summary>
    public static List<(string Value, string Display)> GetAllCategories()
    {
        return Enum.GetValues<ProductCategory>()
            .Select(c => (c.ToString(), GetDisplayName(c)))
            .OrderBy(c => c.Item2)
            .ToList();
    }

    /// <summary>
    /// Get category from string
    /// </summary>
    public static ProductCategory? ParseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        if (Enum.TryParse<ProductCategory>(category, true, out var result))
            return result;

        return null;
    }
}
