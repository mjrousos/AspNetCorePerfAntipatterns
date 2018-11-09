using System;
using System.Buffers;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LOHAllocations.Controllers
{
    // This test API returns the last byte of a large image
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // ~93KB image
        const string ImageSource = "https://blogs.microsoft.com/uploads/2012/08/8867.Microsoft_5F00_Logo_2D00_for_2D00_screen.jpg";
        
        // Use IHttpClientFactory so that HTTP connections are properly pooled
        private readonly IHttpClientFactory _httpClientFactory;

        public TestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // GET api/image/slow
        [HttpGet("slow")]
        public async Task<ActionResult<byte>> GetImageSlowAsync()
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                // By default, HttpClient.GetAsync will load the response body.
                // Normally this is ok, but in hot code paths where a large response
                // is expected, it's better to only read headers initially
                // (using `HttpCompletionOption.ResponseHeadersRead`).
                var response = await client.GetAsync(ImageSource);

                // Allocating large byte[] and string objects on a hot code path
                // will lead to frequent gen 2 GCs and poor performance. These objects
                // should be pooled or cached.
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                return Ok(imageBytes[imageBytes.Length - 1]);
            }
        }

        // GET api/image/slow
        [HttpGet("fast")]
        public async Task<ActionResult<byte>> GetImageFastAsync()
        {
            // Ideally the large object would be cached to avoid both the GC pressure 
            // and the HTTP call. Assuming that isn't an option, though, ArrayPools
            // can reduce GC pressure.
            using (var client = _httpClientFactory.CreateClient())
            {
                // Only read headers initially so that the content can be streamed
                using (var response = await client.GetAsync(ImageSource, HttpCompletionOption.ResponseHeadersRead))
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    // Here a byte[] is rented from the ArrayPool instead of being allocated new
                    var imageBytes = ArrayPool<byte>.Shared.Rent(200*1000);

                    try
                    {
                        int bytesRead, offset = 0;
                        do
                        {
                            bytesRead = await responseStream.ReadAsync(imageBytes, offset, 10000);
                            offset += bytesRead;
                        }
                        while (bytesRead > 0);
                        
                        return Ok(imageBytes[offset - 1]);
                    }
                    finally
                    {
                        // Arrays from ArrayPools must be returned once they're no longer needed
                        ArrayPool<byte>.Shared.Return(imageBytes);
                    }
                }
            }
        }
    }
}
