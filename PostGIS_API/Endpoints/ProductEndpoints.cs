namespace PostGIS_API.Endpoints;

using Npgsql;
using PostGIS_API.Models;

public static class ProductEndpoints
{
    public static WebApplication AddProductEndpoints(this WebApplication app)
    {
        app.MapGet("/products_loaded", ProductsLoaded);
        app.MapGet("/products", GetAllProducts);
        return app;
    }

    static async Task<IResult> ProductsLoaded(NpgsqlDataSource dataSource)
    {
        try
        {
            var query = "SELECT COUNT(*) FROM Products;";
            await using var cmd = dataSource.CreateCommand(query);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return Results.Ok(count);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    static async Task<IResult> GetAllProducts(NpgsqlDataSource dataSource,
        double longitude, double latitude, double maxDistance, double minPrice,
        double maxPrice, int minCalories, int maxCalories)
    {
        try
        {
            var query = $@"
                SELECT
                    p.id, p.name, p.price, p.calories,
                    ST_Distance(
                        s.location,
                        ST_MakePoint(($1), ($2))::geography
                    )/1000 as distance
                FROM 
                    Products AS p 
                    INNER JOIN Stores AS s ON p.store_id = s.id
                WHERE
                    ST_DWithin(
                        s.location,
                        ST_MakePoint(($1), ($2))::geography,
                        ($3) * 1000
                    )
                    AND p.price BETWEEN ($4) AND ($5)
                    AND p.calories BETWEEN ($6) AND ($7)
                ORDER BY 
                    distance;
            ";

            var result = new List<Product>();

            await using var cmd = dataSource.CreateCommand(query);
            cmd.Parameters.AddWithValue(longitude);
            cmd.Parameters.AddWithValue(latitude);
            cmd.Parameters.AddWithValue(maxDistance);
            cmd.Parameters.AddWithValue(minPrice);
            cmd.Parameters.AddWithValue(maxPrice);
            cmd.Parameters.AddWithValue(minCalories);
            cmd.Parameters.AddWithValue(maxCalories);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var prod_id = reader.GetInt32(0);
                var prod_name = reader.GetString(1);
                var prod_price = reader.GetDouble(2);
                var prod_calories = reader.GetInt32(3);
                var distance = reader.GetDouble(4);

                result.Add(new Product(prod_id, prod_name, prod_price, prod_calories, distance));
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}
