using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ChattyDataAccess.Controllers
{
    // This test API calculates MD5 hashes for images associated with particular products in the database
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        static readonly int[] ProductIds = new int[] {
            771, 772, 773, 774, 775, 776, 777, 778, 779, 780, 781, 782, 783, 784, 785, 786, 787, 788, 
            980, 981, 982, 983, 984, 985, 986, 987, 988, 989, 990, 991, 992, 993};
        
        // The slow API will query each product thumbnail individually
        // It is also slower because it retrieves columns that aren't used. Querying for '*'
        // and similar patterns can lead to reduced throughput.
        const string GetThumbnailSlow = @"select ThumbNailPhoto, ThumbNailPhotoFileName, Name
                                          from SalesLT.Product where ProductID = {0}";

        // The fast API will query all product thumbnails at once (and only retrieve a single column of data)
        const string GetThumbnailFast = @"select ThumbNailPhoto from SalesLT.Product where ProductID in ({0})";

        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // GET api/data/slow
        [HttpGet("slow")]
        public async Task<ActionResult<IEnumerable<string>>> GetSlow()
        {
            var hashes = new List<string>();

            var connectionString = _configuration["ConnectionStringBase"].Replace("{PASSWORD}", _configuration["DatabasePassword"]);

            using (var md5 = MD5.Create())
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var id in ProductIds)
                {
                    // In this example, we know the list of product IDs formatted into the command is valid.
                    // In general, though, you should be careful about constructing SQL commands from strings
                    // in case SQL injection attacks are possible.
                    var commandText = string.Format(GetThumbnailSlow, id);
                
                    using (var command = new SqlCommand(commandText, connection))

                    // Executing a SQL command inside of a loop is a red flag
                    // It's important to minimize trips to the database. Most APIs
                    // shouldn't need more than one or maybe two queries.
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var buffer = ArrayPool<byte>.Shared.Rent(100 * 1000);
                            try
                            {
                                var bytesRead = reader.GetBytes(0, 0, buffer, 0, buffer.Length);
                                hashes.Add(Convert.ToBase64String(md5.ComputeHash(buffer, 0, (int)bytesRead)));
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }
                        }
                    }
                }
            }

            return Ok(hashes);
        }

        // GET api/data/fast
        [HttpGet("fast")]
        public async Task<ActionResult<IEnumerable<string>>> GetFast()
        {
            var hashes = new List<string>();

            var connectionString = _configuration["ConnectionStringBase"].Replace("{PASSWORD}", _configuration["DatabasePassword"]);

            using (var md5 = MD5.Create())
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // In this example, we know the list of product IDs formatted into the command is valid.
                // In general, though, you should be careful about constructing SQL commands from strings
                // in case SQL injection attacks are possible.
                var commandText = string.Format(GetThumbnailFast, string.Join(',', ProductIds));

                using (var command = new SqlCommand(commandText, connection))

                // This API is much faster because it retrieves all needed data from the database
                // with a single query.
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent(100 * 1000);
                        try
                        {
                            var bytesRead = reader.GetBytes(0, 0, buffer, 0, buffer.Length);
                            hashes.Add(Convert.ToBase64String(md5.ComputeHash(buffer, 0, (int)bytesRead)));
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            }

            return Ok(hashes);
        }
    }
}
