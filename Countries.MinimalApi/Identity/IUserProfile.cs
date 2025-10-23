namespace Countries.MinimalApi.Identity;

public interface IUserProfile
{
    string Name { get; }
    IEnumerable<string> Roles { get; }
}