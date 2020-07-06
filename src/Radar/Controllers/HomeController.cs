using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Radar.Models;

namespace Radar.Controllers
{
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;


    public class IndexModel
    {
        public IEnumerable<Radar> Radars { get; set; }
    }

    public class Radar
    {
        public string Name { get; set; }
        public string FileLocation { get; set; }
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        static readonly ConcurrentBag<Radar> _radarLocations = new ConcurrentBag<Radar>();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            Populate();

            var model = new IndexModel()
            {
                Radars = _radarLocations
            };

            return View(model);
        }


        public IActionResult File(string fileName)
        {
            Populate();

            var file = _radarLocations.FirstOrDefault(x => x.Name == fileName);

            if (file != null)
            {
                Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}.csv");
                return File(System.IO.File.ReadAllBytes(file.FileLocation), "text/csv", $"{fileName}.csv");
            }
            
            _logger.LogInformation($"did not find {fileName}");
            return NotFound();

        }


        private void Populate()
        {
            if (!_radarLocations.IsEmpty) return;
            
            var cwd = Directory.GetCurrentDirectory();
            var radarFolder = Path.Combine(cwd, "radars");

            _logger.LogInformation($"looking in folder ({radarFolder}) for radars");

            if (!Directory.Exists(radarFolder))
            {
                var message = $"Cannot find the radar folder {radarFolder}";
                _logger.LogInformation(message);
                throw new Exception(message);
            }

            var files = Directory.GetFiles(radarFolder);

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                _radarLocations.Add(new Radar { Name = name, FileLocation = file });
            }

            _logger.LogInformation($"found {_radarLocations.Count} radars");

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
