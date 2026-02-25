using Microsoft.AspNetCore.Mvc;
using WeatherApp.Services;

namespace WeatherApp.ViewComponents;

public class CbcNewsViewComponent : ViewComponent
{
    private readonly ICbcNewsService _news;

    public CbcNewsViewComponent(ICbcNewsService news)
    {
        _news = news;
    }

    public async Task<IViewComponentResult> InvokeAsync(int itemsPerSection = 4)
    {
        var vm = await _news.GetCurrentSectionsAsync(itemsPerSection, HttpContext.RequestAborted);
        return View(vm);
    }
}
