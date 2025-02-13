using Horus.Modules.Core.Application.Extensions;
using Horus.Modules.Shared.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CachedAttribute : ActionFilterAttribute, IAsyncActionFilter
{
    private readonly int _timeToLive;

    public CachedAttribute(int timeToLiveSecs = 60)
    {
        _timeToLive = timeToLiveSecs;
    }


    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var keyFactory = context.HttpContext.RequestServices.GetRequiredService<IRedisKeyFactory>();
        var cacheService = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

        var key = keyFactory.CreateCachingKey(context.HttpContext.Request.Path, context.HttpContext.Request.Query);

        var cachedResult = await cacheService.GetStringAsync(key);
        if (cachedResult != null)
        {
            // Return the cached result to the response
            var result = new ContentResult
            {
                Content = cachedResult,
                ContentType = "application/json",
                StatusCode = 200
            };

            context.Result = result;
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is OkObjectResult okObjectResult)
            await cacheService.SetRecordAsync(key, okObjectResult.Value, TimeSpan.FromSeconds(_timeToLive));
    }
}