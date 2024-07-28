namespace Proximate;

public interface IProxy
{
    string Id { get; }
    string LastResult { get; }
    DateTime LastUpdateTime { get; }
    Task<string> Update();
}