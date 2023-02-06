using System.Text.Json;
using System.Text.Json.Nodes;

using System.Text;
using System.Text.Encodings.Web;

namespace ProjectStructure;

public interface ISerialize
{
    public string SerializeSelf();
}

public interface IRestoreState : ISerialize
{
    public void RestoreState(string json);
}

public interface IPersistable : ISerialize
{
    string GetConfigFileName();

    Task<string> PersistToDisk(string projectPath);

    /// <summary>
    /// full path to the resource (FolderPath + GetConfigFileName)
    /// </summary>
    public string ResourceFullPath { get; }

    /// <summary>
    /// the path to the folder in which the resource is stored
    /// </summary>
    public string FolderPath { get; }

    /// <summary>
    /// a resource folder in case the resource is stored in a folder
    /// this is the case where a single resource is stored as multiple files
    /// </summary>
    public string SubFolder { get; }
}

public interface IAccept<TResource>
{
    ValueTask Accept(IVisitor<TResource> visitor);
}

public interface IVisitor<TResource>
{
    ValueTask Visit(TResource target);
}

public interface IResource : IAccept<IResource>
{
    public Guid Id { get; }
    public int Version { get; }
}

public interface IResourceContainer : IResource
{
    public List<IResource> Resources { get; set; }

    public void AddResource(IResource resource);
}

public abstract class Resource : IResource, ISerialize, IPersistable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public virtual string Name { get; set; }

    public int Version { get; set; } = 1;

    public string FolderPath { get; set; }

    public virtual string SubFolder { get; set; }

    public string ResourceFullPath => Path.Combine(FolderPath, GetConfigFileName());

    public virtual ValueTask Accept(IVisitor<IResource> visitor) => visitor.Visit(this);

    public abstract string GetConfigFileName();

    public virtual async Task<string> PersistToDisk(string projectPath)
    {
        var path = projectPath;
        if (!string.IsNullOrWhiteSpace(SubFolder))
        {
            path = Path.Combine(path, SubFolder);
        }

        Directory.CreateDirectory(projectPath);

        FolderPath = path;

        var content = SerializeSelf();

        await File.WriteAllTextAsync(ResourceFullPath, content, Encoding.UTF8);

        return ResourceFullPath;
    }

    public abstract string SerializeSelf();
}

public abstract class ResourceContainer : Resource, IResourceContainer
{
    public List<IResource> Resources { get; set; } = new();

    protected abstract IEnumerable<Type> _allowedSubResources { get; }

    public override ValueTask Accept(IVisitor<IResource> visitor)
    {
        foreach (var resource in Resources)
        {
            visitor.Visit(resource);
        }

        return visitor.Visit(this);
    }

    public virtual void AddResource(IResource resource)
    {
        if (!_allowedSubResources.Any(x => x == resource.GetType()))
        {
            throw new ArgumentException("Can not add unsupported resouce");
        }
        if (Resources.Any(x => x.Id == resource.Id))
        {
            throw new ArgumentException($"can not add resource with same Id {resource.Id}");
        }

        Resources.Add(resource);
    }

    public override async Task<string> PersistToDisk(string projectPath)
    {
        var path = projectPath;

        if (!string.IsNullOrWhiteSpace(SubFolder))
        {
            path = Path.Combine(path, SubFolder);
        }

        FolderPath = path;
        Directory.CreateDirectory(path);

        // handle sub-resources
        foreach (var resource in Resources.OfType<IPersistable>())
        {
            await resource.PersistToDisk(path);
        }

        var content = SerializeSelf();

        await File.WriteAllTextAsync(ResourceFullPath, content, Encoding.UTF8);

        return ResourceFullPath;
    }
}

public static class GlobalUsings
{
    public static System.Text.Json.JsonSerializerOptions JsonSerializerOptions => new JsonSerializerOptions
    {
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding#serialize-all-characters
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
