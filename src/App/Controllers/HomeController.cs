using App.Data;
using App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace App.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Index()
    { 
        return View(Store.Apps.Keys);
    }

    [HttpGet("/{slug}")]
    public IActionResult GetApp([FromRoute] string slug)
    {
        var app = Store.Apps.GetOrAdd(slug, new DbApp());
        return View("App", new KeyValuePair<string, DbApp>(slug, app));
    }

    [HttpPost("/{slug}")]
    public IActionResult Post([FromRoute] string slug, [FromForm] Feedback feedback)
    {
        var app = Store.Apps.GetOrAdd(slug, new DbApp());
        app.Feedbacks.Add(feedback);
        return PartialView("_Feedback", feedback);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
