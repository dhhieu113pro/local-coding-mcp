namespace LocalCodingMcp.Services;

public sealed record SkillUpdateStatus(
    string Name,
    string SourceUrl,
    string? InstalledSha256,
    string? RemoteSha256,
    string Status,
    string? Message = null);

public sealed record SkillUpdateResult(
    string Name,
    string? OldSha256,
    string NewSha256,
    bool Changed,
    SkillDocument Skill);

public sealed class RemoteSkillService
{
    private readonly SkillStore _skills;
    private readonly RemoteSkillFetcher _fetcher;

    public RemoteSkillService(SkillStore skills, RemoteSkillFetcher fetcher)
    {
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
    }

    public async Task<SkillDocument> InstallAsync(
        string source,
        bool enabled = true,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var fetched = await _fetcher.FetchAsync(source, cancellationToken: cancellationToken);
        var frontMatter = SkillDocumentParser.Parse(fetched.Content);
        if (!string.IsNullOrWhiteSpace(name)
            && !string.Equals(name, frontMatter.Name, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Requested skill name '{name}' does not match front-matter name '{frontMatter.Name}'.");
        }

        return _skills.InstallRemote(fetched.Content, frontMatter, fetched, enabled);
    }

    public async Task<IReadOnlyList<SkillUpdateStatus>> CheckUpdatesAsync(
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SkillDocument> candidates;
        if (!string.IsNullOrWhiteSpace(name))
        {
            candidates = [_skills.Get(name)];
        }
        else
        {
            candidates = _skills.List()
                .Where(skill => !string.IsNullOrWhiteSpace(skill.SourceUrl) && !skill.BuiltIn)
                .Select(skill => _skills.Get(skill.Name))
                .ToArray();
        }

        var results = new List<SkillUpdateStatus>(candidates.Count);
        foreach (var skill in candidates)
        {
            if (skill.BuiltIn || string.IsNullOrWhiteSpace(skill.SourceUrl))
            {
                results.Add(new SkillUpdateStatus(
                    skill.Name,
                    skill.SourceUrl ?? string.Empty,
                    skill.ContentSha256,
                    null,
                    "unavailable",
                    skill.BuiltIn ? "Built-in skills are not remotely updateable." : "Skill has no recorded remote source."));
                continue;
            }

            try
            {
                var remote = await _fetcher.FetchAsync(skill.SourceUrl, cancellationToken: cancellationToken);
                var status = string.Equals(skill.ContentSha256, remote.ContentSha256, StringComparison.OrdinalIgnoreCase)
                    ? "up_to_date"
                    : "update_available";
                results.Add(new SkillUpdateStatus(
                    skill.Name, skill.SourceUrl, skill.ContentSha256, remote.ContentSha256, status));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException)
            {
                results.Add(new SkillUpdateStatus(
                    skill.Name, skill.SourceUrl, skill.ContentSha256, null, "unavailable", ex.Message));
            }
        }

        return results;
    }

    public async Task<SkillUpdateResult> UpdateFromSourceAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var current = _skills.Get(name);
        if (current.BuiltIn)
            throw new InvalidOperationException($"Built-in skill '{name}' cannot be updated from a remote source.");
        if (string.IsNullOrWhiteSpace(current.SourceUrl))
            throw new InvalidOperationException($"Skill '{name}' has no recorded remote source.");

        var fetched = await _fetcher.FetchAsync(current.SourceUrl, cancellationToken: cancellationToken);
        var frontMatter = SkillDocumentParser.Parse(fetched.Content);
        if (!string.Equals(name, frontMatter.Name, StringComparison.Ordinal))
            throw new InvalidDataException("Upstream skill name does not match the installed skill name.");

        var changed = !string.Equals(current.ContentSha256, fetched.ContentSha256, StringComparison.OrdinalIgnoreCase);
        var refreshed = _skills.ReplaceRemote(name, fetched.Content, frontMatter, fetched);
        return new SkillUpdateResult(name, current.ContentSha256, fetched.ContentSha256, changed, refreshed);
    }
}
