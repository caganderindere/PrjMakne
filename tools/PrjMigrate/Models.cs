using System.Text.Json.Serialization;

namespace PrjMigrate;

// Shapes of the public WooCommerce Store API (/wp-json/wc/store/v1) and the WordPress pages API.
public sealed record WpImage([property: JsonPropertyName("src")] string Src);

public sealed record WpTerm(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("name")] string Name);

public sealed record WpCategory(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("permalink")] string Permalink,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("image")] WpImage? Image);

public sealed record WpProduct(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("permalink")] string Permalink,
    [property: JsonPropertyName("short_description")] string? ShortDescription,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("images")] IReadOnlyList<WpImage> Images,
    [property: JsonPropertyName("categories")] IReadOnlyList<WpTerm> Categories);

public sealed record WpRendered([property: JsonPropertyName("rendered")] string Rendered);

public sealed record WpPage(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("link")] string Link,
    [property: JsonPropertyName("title")] WpRendered Title,
    [property: JsonPropertyName("content")] WpRendered Content);
