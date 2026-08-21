namespace LocalCodingMcp.Services;

/// <summary>
/// Applies simple unified-diff style patches to text files.
/// Supports basic @@ hunks for single-file edits.
/// </summary>
public static class PatchApplier
{
    public static string Apply(string originalContent, string patch)
    {
        if (string.IsNullOrWhiteSpace(patch))
            throw new ArgumentException("Patch cannot be empty.");

        var lines = originalContent.Replace("\r\n", "\n").Split('\n').ToList();
        var patchLines = patch.Replace("\r\n", "\n").Split('\n');

        // Very simple hunk parser: look for @@ -start,count +start,count @@
        // and then apply - / + lines.
        var i = 0;
        while (i < patchLines.Length)
        {
            var line = patchLines[i];
            if (!line.StartsWith("@@"))
            {
                i++;
                continue;
            }

            // Parse @@ -oldStart,oldCount +newStart,newCount @@
            var parts = line.Split(' ');
            if (parts.Length < 3)
            {
                i++;
                continue;
            }

            var oldPart = parts[1]; // -12,5
            if (!oldPart.StartsWith("-"))
            {
                i++;
                continue;
            }

            var oldStart = int.Parse(oldPart[1..].Split(',')[0]);
            var index = oldStart - 1; // 1-based to 0-based

            i++;
            var toRemove = new List<string>();
            var toAdd = new List<string>();

            while (i < patchLines.Length && !patchLines[i].StartsWith("@@"))
            {
                var pl = patchLines[i];
                if (pl.StartsWith("-") && !pl.StartsWith("---"))
                    toRemove.Add(pl[1..]);
                else if (pl.StartsWith("+") && !pl.StartsWith("+++"))
                    toAdd.Add(pl[1..]);
                // context lines (start with space) are ignored for positioning
                i++;
            }

            // Remove old lines
            foreach (var _ in toRemove)
            {
                if (index >= 0 && index < lines.Count)
                    lines.RemoveAt(index);
            }

            // Insert new lines
            for (var j = 0; j < toAdd.Count; j++)
            {
                lines.Insert(index + j, toAdd[j]);
            }
        }

        return string.Join("\n", lines);
    }
}
