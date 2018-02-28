using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;

namespace n1mmlistener
{
    [Route("api")]
    public class ApiController : Controller
    {
        [HttpGet("leaderboard/TotalQsos")]
        public IActionResult GetTotalQsosLeaderboard()
        {
            var repo = new ContactDbRepo();

            var sw = Stopwatch.StartNew();
            List<LeaderboardRow> list = repo.GetTotalQsoLeaderboard();
            Request.HttpContext.Response.Headers.Add("X-Db-Took-ms", sw.ElapsedMilliseconds.ToString());

            return Ok(list);
        }

        [HttpGet("leaderboard/TotalIsMult1Contacts")]
        public IActionResult GetTotalIsMult1ContactsLeaderboard()
        {
            var repo = new ContactDbRepo();

            var sw = Stopwatch.StartNew();
            List<LeaderboardRow> list = repo.GetIsMulti1Leaderboard();
            Request.HttpContext.Response.Headers.Add("X-Db-Took-ms", sw.ElapsedMilliseconds.ToString());

            return Ok(list);
        }
    }

    public class LeaderboardRow
    {
        public string Operator { get; set; }
        public int Count { get; set; }
    }
}