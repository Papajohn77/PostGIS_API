namespace PostGIS_API.Endpoints;

using Npgsql;
using PostGIS_API.Models;

public static class StoreEndpoints
{
    public static WebApplication AddStoreEndpoints(this WebApplication app)
    {
        app.MapGet("/stores", GetAllStores);
        return app;
    }

    static async Task<IResult> GetAllStores(NpgsqlDataSource dataSource,
        double longitude, double latitude, double maxDistance)
    {
        try
        {
            var query = $@"
                SELECT
                    s.id, s.name,
                    ST_Distance(
                        s.location,
                        ST_MakePoint(($1), ($2))::geography
                    )/1000 AS distance
                FROM 
                    Stores AS s
                WHERE
                    ST_DWithin(
                        s.location,
                        ST_MakePoint(($1), ($2))::geography,
                        ($3) * 1000
                    )
                ORDER BY 
                    distance;
            ";

            var result = new List<Store>();

            await using var cmd = dataSource.CreateCommand(query);
            cmd.Parameters.AddWithValue(longitude);
            cmd.Parameters.AddWithValue(latitude);
            cmd.Parameters.AddWithValue(maxDistance);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var store_id = reader.GetInt32(0);
                var store_name = reader.GetString(1);
                var distance = reader.GetDouble(2);

                result.Add(new Store(store_id, store_name, distance));
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}
