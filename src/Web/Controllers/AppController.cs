using Microsoft.AspNetCore.Mvc;
using Web.Data;

namespace Web.Controllers;

public class AppController(Db db) : Controller
{
    private readonly Db db = db;

    public const string AppsPath = "/apps";
    
    [HttpGet(AppsPath)]
    public IActionResult Apps()
    {
        return View();
    }

    [HttpGet("/apps/{id}")]
    public IActionResult OneApp([FromRoute] string id)
    {
        var app = db.Apps.FirstOrDefault(a => a.Id == id);
        if(app == null) return NotFound();
        
        return View();
    }
}
