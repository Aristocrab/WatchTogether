using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WatchTogether.Controllers
{
    public class WatchTogetherController : Controller
    {
        [Route("/w")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/r")]
        public IActionResult Room(string id)
        {
            if(!string.IsNullOrWhiteSpace(id))
            {
                return View(model: id);
            }
            else
            {
                return Redirect("/");
            }
        }
    }
}
