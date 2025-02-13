using Microsoft.AspNetCore.Http;

namespace Horus.Modules.Shared.Contracts.Services;

public interface IRedisKeyFactory
{
    public string CreateCachingKey(PathString path, IQueryCollection query);
    public string CreateGlobalInstancePrefix();
    public string CreateAuditPrefix();
}