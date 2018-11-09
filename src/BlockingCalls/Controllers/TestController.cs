using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BlockingCalls.Controllers
{
    // This test API returns the name and category name for all products in the database
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        const string Query = @"select product.Name,category.Name from SalesLT.Product as product 
                               join SalesLT.ProductCategory as category 
	                             on product.ProductCategoryID = category.ProductCategoryID";

        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        // GET api/data/slow
        [HttpGet("slow")]
        public ActionResult<IEnumerable<string>> GetSlow()
        {
            var results = new List<string>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // This could be (and should be) async
                connection.Open();

                using (var command = new SqlCommand(Query, connection))
                {
                    // Both ExecuteReader and Read should be async
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        results.Add($"{reader.GetString(0)} ({reader.GetString(1)})");
                    }
                }
            }

            // Add an extra 100ms delay since the SQL query can be quite fast when run in Azure
            // Using Task.Wait should be a red flag since it's turning an asynchronous task into
            // a synchronous one!
            Task.Delay(100).Wait();

            return Ok(results);
        }

        // GET api/data/fast
        [HttpGet("fast")]
        public async Task<ActionResult<IEnumerable<string>>> GetFast()
        {
            var results = new List<string>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(Query, connection))
                {
                    var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        results.Add($"{reader.GetString(0)} ({reader.GetString(1)})");
                    }
                }
            }

            // Add an extra 100ms delay since the SQL query can be quite fast when run in Azure
            await Task.Delay(100);

            return Ok(results);
        }
    }
}
