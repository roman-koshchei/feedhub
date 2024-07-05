using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Lib;

/// <summary>
/// Class to represent partial view in code.
/// Provides single source of truth, like path to file and model type.
/// </summary>
/// <typeparam name="T">Model type, like props</typeparam>
/// <param name="Path">Path to view file (.cshtml)</param>
public record View<T>(string Path)
{
    /// <summary>
    /// Provides IActionResult that can be returned from controller.
    /// Used inside of controllers.
    /// </summary>
    public PartialViewResult PartialResult(Controller controller, T model)
    {
        return controller.PartialView(Path, model);
    }

    public ViewResult ViewResult(Controller controller, T model)
    {
        return controller.View(Path, model);
    }

    /// <summary>
    /// Renders partial in Views.
    /// </summary>
    public Task<IHtmlContent> Render(IHtmlHelper htmlHelper, T model)
    {
        return htmlHelper.PartialAsync(Path, model);
    }
}


public record View(string Path)
{
    public PartialViewResult PartialResult(Controller controller)
    {
        return controller.PartialView(Path);
    }

    public ViewResult ViewResult(Controller controller)
    {
        return controller.View(Path);
    }

    public Task<IHtmlContent> RenderAsPartial(IHtmlHelper htmlHelper)
    {
        return htmlHelper.PartialAsync(Path);
    }
}