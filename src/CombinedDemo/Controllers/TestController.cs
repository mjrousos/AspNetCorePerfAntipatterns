using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CombinedDemo.Controllers
{
    // This test API retrieves MD5 hashes for product images for all products
    // in a given category that are currently in stock
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // Query string for retrieving all products in the 'Mountain Bikes' category
        const string GetMountainBikesQuery = @"select product.ProductID from SalesLT.Product where ProductCategoryID = 5";

        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // This action exhibits all three performance problems demonstrated in the other samples:
        //   - Not async
        //   - Chatty DB communication
        //   - Excessive large object allocation (and cleanup)
        [HttpGet("Test1")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes1()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                // This should, of course, be async
                connection.Open();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                
                // Both ExecuteReader and Read should be async
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();
            
            // Filter for in-stock products
            foreach (var id in productIDs)
            {
                // Task.Result should be a red flag. This needs to be async
                if (ProductIsInStock(id).Result)
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                connection.Open();

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in inStockProductIds)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    // Executing a SQL command in a for loop is a red flag
                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = command.ExecuteReader())
                    using (var md5 = MD5.Create())
                    {
                        while (reader.Read())
                        {
                            // Allocating a new byte[] for every product will result in a lot of memory
                            // pressure and many gen 2 garbage collections
                            var thumbnail = new byte[100 * 1000];
                            var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);

                            // Computing an MD5 hash can be slow but is an example of the sort of compute-bound
                            // work that is hard to optimize away without caching results or completing the calculations
                            // out-of-process
                            hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                        }
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        // Use ArrayPool<byte> instead of allocating large byte[]s
        [HttpGet("Test2")]
        public ActionResult<IEnumerable<string>> GetInStockThumbnailHashes2()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                // This should, of course, be async
                connection.Open();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                
                // Both ExecuteReader and Read should be async
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();
            
            // Filter for in-stock products
            foreach (var id in productIDs)
            {
                // Task.Result should be a red flag. This needs to be async
                if (ProductIsInStock(id).Result)
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                connection.Open();

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in inStockProductIds)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = command.ExecuteReader())
                    using (var md5 = MD5.Create())
                    {
                        while (reader.Read())
                        {
                            // Using a byte[] rented from an ArrayPool avoids
                            // the frequent allocation and clean-up of large objects
                            // that the previous version of this API had.
                            var thumbnail = ArrayPool<byte>.Shared.Rent(100 * 1000);
                            try
                            {
                                var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                                hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                            }
                            finally
                            {
                                // Arrays from ArrayPools need returned once they're no longer needed
                                ArrayPool<byte>.Shared.Return(thumbnail);
                            }
                        }
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        // Make async
        [HttpGet("Test3")]
        public async Task<ActionResult<IEnumerable<string>>> GetInStockThumbnailHashes3()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            var inStockProductIds = new List<int>();

            // Filter for in-stock products
            foreach (var id in productIDs)
            {
                // Calling ProductIsInStock asynchronously is important
                // as it allows other requests to be processed while waiting
                // for the remote service to return.
                if (await ProductIsInStock(id))
                {
                    inStockProductIds.Add(id);
                }
            }

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // Iterate through in-stock products, retrieve images and store hashes
                foreach (var id in inStockProductIds)
                {
                    var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID = {id}";

                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    using (var md5 = MD5.Create())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Using a byte[] rented from an ArrayPool avoids
                            // the frequent allocation and clean-up of large objects
                            // that the previous version of this API had.
                            var thumbnail = ArrayPool<byte>.Shared.Rent(100 * 1000);
                            try
                            {
                                var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                                hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                            }
                            finally
                            {
                                // Arrays from ArrayPools need returned once they're no longer needed
                                ArrayPool<byte>.Shared.Return(thumbnail);
                            }
                        }
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        // Consolidate SQL chatter
        [HttpGet("Test4")]
        public async Task<ActionResult<IEnumerable<string>>> GetInStockThumbnailHashes4()
        {
            // List of hashes to return
            var hashes = new List<string>();

            // Product IDs
            var productIDs = new List<int>();

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // Store all returned product IDs
                using (var command = new SqlCommand(GetMountainBikesQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        productIDs.Add(reader.GetInt32(0));
                    }
                }

                // Close the connection for now since determining which products 
                // are in stock can take a moment and we don't want to exhaust SQL connections
                connection.Close();
            }

            // Ideally, this API would only access SQL once, but the fact that the stock
            // checking service is external complicates matters. If we knew that products were usually
            // in stock, it might be quicker to just retrieve product photos for all category
            // and filter out those that aren't in stock afterwards. Since that change could have either
            // positive or negative performance impact depending on the data set, performance testing
            // would be needed to determine which is best.
            //
            // In this case, we continue the general pattern of retrieving IDs, filtering based on
            // availability, and then retrieving photos in order to demonstrate how much optimization
            // can be done even without broader architectural changes.

            var inStockProductIds = new ConcurrentBag<int>();

            // There is some small optimization in low-load scenarios to checking whether 
            // products are in stock in parallel. In high-load scenarios, this is not likely
            // to make a large difference compared to just await'ing each ProductIsInStock call
            // in series (since there will be plenty of other requests to process while waiting
            // for those calls to return)
            await Task.WhenAll(productIDs.Select(async id =>
            {
                if (await ProductIsInStock(id))
                {
                    inStockProductIds.Add(id);
                }
            }));

            using (var connection = new SqlConnection(_configuration["ConnectionString"]))
            {
                // Open the SQL connection
                await connection.OpenAsync();

                // This improved SQL query retrieves photos for all relevant products at once instead of
                // using multiple queries to get the same information
                var commandText = $"select ThumbNailPhoto from SalesLT.Product where ProductID in ({string.Join(',', inStockProductIds)})";

                // Retrieve thumbnails (and compute hashes) for in-stock products
                using (var command = new SqlCommand(commandText, connection))
                using (var reader = await command.ExecuteReaderAsync())
                using (var md5 = MD5.Create())
                {
                    // Since there will be multiple rows in the query's results,
                    // we can use a single rented byte[] for all of them
                    var thumbnail = ArrayPool<byte>.Shared.Rent(100 * 1000);

                    try
                    {
                        while (await reader.ReadAsync())
                        {
                            var bytesRead = reader.GetBytes(0, 0, thumbnail, 0, thumbnail.Length);
                            hashes.Add(Convert.ToBase64String(md5.ComputeHash(thumbnail, 0, (int)bytesRead)));
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(thumbnail);
                    }
                }

                connection.Close();
            }

            return Ok(hashes);
        }

        private async Task<bool> ProductIsInStock(int productId)
        {
            // This method mimcs a call to another service to check stock
            await Task.Delay(30);

            // For test purposes, just say product IDs in the 700s or 900s are in stock and others aren't
            var productSeries = productId / 100;
            return productSeries == 7 || productSeries == 9;
        }
    }
}
