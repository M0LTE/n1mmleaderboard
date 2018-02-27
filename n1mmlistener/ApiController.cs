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
        [HttpGet("leaderboard/TotalQsos")]
        public IActionResult GetTotalQsosLeaderboard()
        {
            var repo = new ContactDbRepo();

            List<LeaderboardRow> list = repo.GetTotalQsoLeaderboard();

            return Ok(list);
        }

        [HttpGet("leaderboard/TotalIsMult1Contacts")]
        public IActionResult GetTotalIsMult1ContactsLeaderboard()
        {
            var repo = new ContactDbRepo();

            List<LeaderboardRow> list = repo.GetIsMulti1Leaderboard();

            return Ok(list);
        }
    }

    public class LeaderboardRow
    {
        public string Operator { get; set; }
        public int Count { get; set; }
    }
}