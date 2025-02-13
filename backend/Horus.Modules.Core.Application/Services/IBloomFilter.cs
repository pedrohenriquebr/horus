namespace Horus.Modules.Core.Application.Services;

public interface IBloomFilter
{
    public bool MaybeContains(string data);
    public void Add(string data);
}