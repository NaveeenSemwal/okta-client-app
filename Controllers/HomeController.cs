using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using client_okta.Models;
using Microsoft.AspNetCore.Authorization;

namespace client_okta.Controllers
{

    /// <summary>
    /// https://stackoverflow.com/questions/28846695/oauth-and-wcf-soap-service
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IApiService apiService;

        


        public HomeController(IApiService apiService)
        {
            this.apiService = apiService;
        }


        public async Task<IActionResult> Index()
        {



          
            var values = await apiService.GetValues();
            return Content(values[0]);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
