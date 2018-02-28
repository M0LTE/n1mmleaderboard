using Microsoft.AspNetCore.Mvc;
using System;
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
            InsertHeader(sw);

            return Ok(list);
        }

        [HttpGet("leaderboard/TotalIsMult1Contacts")]
        public IActionResult GetTotalIsMult1ContactsLeaderboard()
        {
            var repo = new ContactDbRepo();

            var sw = Stopwatch.StartNew();
            List<LeaderboardRow> list = repo.GetIsMulti1Leaderboard();
            InsertHeader(sw);

            return Ok(list);
        }

        [HttpGet("leaderboard/qsorate/{mins}")]
        public IActionResult GetQsoRateLeaderboard(int mins)
        {
            var repo = new ContactDbRepo();

            var sw = Stopwatch.StartNew();
            List<LeaderboardRow> list = repo.GetQsoRateLeaderboard(mins);
            InsertHeader(sw);

            return Ok(list);
        }

        [HttpGet("leaderboard/qsopeak")]
        public IActionResult GetPeakRateLeaderboard()
        {
            var repo = new ContactDbRepo();

            var sw = Stopwatch.StartNew();
            List<LeaderboardRow> list = repo.GetPeakRateLeaderboard();
            InsertHeader(sw);

            return Ok(list);
        }

        void InsertHeader(Stopwatch sw)
        {
            Request.HttpContext.Response.Headers.Add("X-Crunching-Time-ms", sw.ElapsedMilliseconds.ToString());
        }
    }


    public class LeaderboardRow
    {
        public string Operator { get; set; }
        public int Count { get; set; }
    }
}