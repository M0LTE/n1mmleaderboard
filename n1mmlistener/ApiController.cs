using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace n1mmlistener
{
    [Route("api")]
    public class ApiController : Controller
    {
        [HttpGet("leaderboard/{sort}")]
        public IActionResult Get(string sort)
        {
            var list = Program.State;
            LeaderboardRow[] sorted;

            if (sort == nameof(LeaderboardRow.TotalQsos))
            {
                sorted = list.OrderByDescending(l => l.TotalQsos).ToArray();
            }
            else if (sort == nameof(LeaderboardRow.TotalIsMult1Contacts))
            {
                sorted = list.OrderByDescending(l => l.TotalIsMult1Contacts).ToArray();
            }
            else
            {
                sorted = list.ToArray();
            }

            return Ok(sorted);
        }
    }

    public class LeaderboardRow
    {
        public string Op { get; set; }
        public int TotalQsos { get; set; }
        public int TotalIsMult1Contacts { get; set; }
    }
}