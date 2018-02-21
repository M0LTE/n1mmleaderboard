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
        [HttpGet("leaderboard")]
        public IActionResult Get()
        {
            return Ok(Program.State);
        }
    }

    public class LeaderboardRow
    {
        public string Op { get; set; }
        public int TotalQsos { get; set; }
    }
}