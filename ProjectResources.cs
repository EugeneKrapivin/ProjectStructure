using System.Text.Json;
using System.Text.Json.Nodes;

using Fluid;

namespace ProjectStructure;

public class Project : ResourceContainer
{
    public Project(string name)
    {
        Name = name;
        SubFolder = name;
    }
    protected override IEnumerable<Type> _allowedSubResources => new[]
    {
        typeof(SiteConfigResource),
        typeof(SiteSchemaCollectionResource)
    };

    public override string GetConfigFileName() => "site.project.json";

    public override string SerializeSelf()
    {
        return System.Text.Json.JsonSerializer.Serialize(new PersistenceModel
        {
            Id = Id,
            Name = Name,
            Version = Version
        }, GlobalUsings.JsonSerializerOptions);
    }

    private class PersistenceModel
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
    }
}

public class Solution : ResourceContainer
{
    public Solution(string name)
    {
        Name = name;
    }
    protected override IEnumerable<Type> _allowedSubResources => new[]
    {
        typeof(Project)
    };

    public override string GetConfigFileName() => "solution.config.json";

    public override void AddResource(IResource resource)
    {
        if (resource is Project site)
            base.AddResource(site);
        else throw new ArgumentException("solution expects only Project", nameof(resource));
    }

    public override string SerializeSelf()
    {
        return System.Text.Json.JsonSerializer.Serialize(new PersistenceModel
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Sites = Resources.Cast<Project>().Select(x => x.Name).ToArray()
        }, GlobalUsings.JsonSerializerOptions);
    }

    private class PersistenceModel
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string[] Sites { get; set; }
    }
}

public class SiteConfigResource : Resource, IRestoreState
{
    public string BaseDomain { get; set; }

    public string DataCenter { get; set; }

    public string[] TrustedSiteUrls { get; set; }

    public string[] Tags { get; set; }

    public string Description { get; set; }

    public override string GetConfigFileName() => "site.config.json";

    public void RestoreState(string json)
    {
        var state = JsonSerializer.Deserialize<SiteConfigResource>(json);

        Id = state.Id;
        Version = state.Version;
        BaseDomain = state.BaseDomain;
        DataCenter = state.DataCenter;
        Description = state.Description;
        Tags = state.Tags;
        TrustedSiteUrls = state.TrustedSiteUrls;
    }

    public override string SerializeSelf()
    {
        return System.Text.Json.JsonSerializer.Serialize(new PersistenceModel
        {
            Id = Id,
            Version = Version,
            BaseDomain = BaseDomain,
            DataCenter = DataCenter,
            Description = Description,
            Tags = Tags,
            TrustedSiteUrls = TrustedSiteUrls
        }, GlobalUsings.JsonSerializerOptions);
    }

    private class PersistenceModel
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string BaseDomain { get; set; }
        public string DataCenter { get; set; }
        public string[] TrustedSiteUrls { get; set; }
        public string[] Tags { get; set; }
        public string Description { get; set; }
    }
}

public class SiteSchemaCollectionResource : ResourceContainer
{
    protected override IEnumerable<Type> _allowedSubResources => new[] { typeof(SchemaResource) };

    public override string GetConfigFileName() => "schema.config.json";

    public override string SubFolder { get; set; } = "schemas";

    public override string SerializeSelf()
    {
        return JsonSerializer.Serialize(new PersistenceModel
        {
            Schemas = Resources
                .OfType<SchemaResource>()
                .Select(x => Path.GetRelativePath(this.FolderPath, x.ResourceFullPath))
                .ToList()
        }, GlobalUsings.JsonSerializerOptions);
    }

    private class PersistenceModel
    {
        public List<string> Schemas { get; set; } = new();
    }
}

public class SchemaResource : Resource
{
    public string SchemaUri { get; set; }
    public string Schema { get; set; }

    public override string GetConfigFileName()
    {
        return $"{Name}.schema.json";
    }

    public override string SerializeSelf()
    {
        var schema = JsonObject.Parse(Schema);
        schema["$schema"] = SchemaUri;

        return schema.ToJsonString(GlobalUsings.JsonSerializerOptions);
    }
}

public class ScreenSetsResouce : ResourceContainer
{
    protected override IEnumerable<Type> _allowedSubResources => new[] { typeof(ScreenSetResource) };

    public override string SubFolder { get; set; } = "screensets";

    public override string GetConfigFileName() => "screensets.config.json";

    public override string SerializeSelf()
    {
        return JsonSerializer.Serialize(new PersistenceModel
        {
            ScreenSets = Resources.Cast<ScreenSetResource>().Select(x => x.Name).ToArray()
        });
    }

    private class PersistenceModel
    {
        public string[] ScreenSets { get; set; }
    }
}

public class ScreenSetResource : Resource, IRestoreState
{
    public string ScreenSetId { get; set; }

    public required string Html { get; set; }

    public string Css { get; set; }

    public string Javascript { get; set; }

    public Dictionary<string, Dictionary<string, string>> Translations { get; set; }

    public string RawTranslations { get; set; }

    public long CompressionType { get; set; }

    public ScreenSetResource(string screenSetId)
    {
        ScreenSetId = screenSetId;
        SubFolder = screenSetId;
    }
    public override string GetConfigFileName() => $"{Name}.screenset.json";

    public override async Task<string> PersistToDisk(string projectPath)
    {
        FolderPath = FolderPath = Path.Combine(projectPath, SubFolder);
        Directory.CreateDirectory(FolderPath);
        var configPath = Path.Combine(FolderPath, $"{ScreenSetId}.config.json");
        await File.WriteAllTextAsync(configPath, SerializeSelf());
        await File.WriteAllTextAsync(Path.Combine(FolderPath, $"{ScreenSetId}.html"), Html);
        await File.WriteAllTextAsync(Path.Combine(FolderPath, $"{ScreenSetId}.css"), Css);
        await File.WriteAllTextAsync(Path.Combine(FolderPath, $"{ScreenSetId}.js"), Javascript);

        return configPath;
    }

    public override string SerializeSelf()
        => JsonSerializer.Serialize(new PersistenceModel
        {
            ScreenSetId = ScreenSetId,
            Translations = Translations,
            RawTranslations = RawTranslations,
            CompressionType = CompressionType
        }, GlobalUsings.JsonSerializerOptions);

    public void RestoreState(string json)
    {
        var state = JsonSerializer.Deserialize<PersistenceModel>(json);

        ScreenSetId = state.ScreenSetId;
        Translations = state.Translations;
        RawTranslations = state.RawTranslations;
        CompressionType = state.CompressionType;
    }

    private class PersistenceModel
    {
        public string ScreenSetId { get; set; }
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; }
        public string RawTranslations { get; set; }
        public long CompressionType { get; set; }
    }
}

public class TemplatingVisitor : IVisitor<IResource>
{
    private readonly JsonNode _model;
    private readonly FluidParser _parser;
    private readonly TemplateContext _context;

    public TemplatingVisitor(JsonObject model)
    {
        _model = model;
        _parser = new FluidParser();

        var tmpOpts = new TemplateOptions();
        tmpOpts.MemberAccessStrategy.Register<JsonNode, object>((source, name) => source[name]);

        _context = new TemplateContext(model, tmpOpts);
    }

    public ValueTask Visit(IResource target)
    {
        if (target is IRestoreState s)
        {
            var serialized = s.SerializeSelf();

            if (_parser.TryParse(serialized, out var template, out var error))
            {
                var rendered = template.Render(_context);
                s.RestoreState(rendered);
            }
            else
            {
                Console.WriteLine($"Error: {error}");
            }
        }

        return ValueTask.CompletedTask;
    }
}