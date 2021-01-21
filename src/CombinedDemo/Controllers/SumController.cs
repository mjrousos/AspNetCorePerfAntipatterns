using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace CombinedDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SumController : ControllerBase
    {
        [HttpGet("{input}")]
        public ActionResult<long> GetSumOneToInput([FromRoute] int input)
        {
            // This should, of course, guard against negative inputs. But supposing it doesn't...
            // if (input < 0)
            // {
            //     return BadRequest("Input must be greater than or equal to zero");
            // }

            return Ok(CalculateSum(input));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long CalculateSum(long input) =>
            input switch
            {
                0 => 0,
                _ => CalculateSum(input - 1) + input
            };
    }
}
