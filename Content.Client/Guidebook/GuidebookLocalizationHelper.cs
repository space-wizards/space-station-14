using System;
using System.Collections.Generic;
using System.Globalization;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook;

public static class GuidebookLocalizationHelper
{
    private static readonly ResPath GuidebookRoot = new("/ServerInfo/Guidebook");

    public static ResPath ResolveLocalizedGuidebookPath(ResPath path, CultureInfo? culture, IResourceManager resourceManager)
    {
        if (culture == null || !path.TryRelativeTo(GuidebookRoot, out var relativeNullable) || relativeNullable is null)
        {
            return path;
        }

        var relative = relativeNullable.Value;
        var relativePath = relative.IsSelf ? string.Empty : relative.CanonPath.TrimStart('/');
        var seenCultures = new HashSet<string>();
        var currentCulture = culture;

        while (true)
        {
            var cultureName = currentCulture.Name;
            if (!string.IsNullOrEmpty(cultureName) && seenCultures.Add(cultureName))
            {
                var localizedPath = string.IsNullOrEmpty(relativePath)
                    ? new ResPath($"{GuidebookRoot.CanonPath}/{cultureName}")
                    : new ResPath($"{GuidebookRoot.CanonPath}/{cultureName}/{relativePath}");

                if (resourceManager.ContentFileExists(localizedPath))
                {
                    return localizedPath;
                }
            }

            if (currentCulture.Equals(CultureInfo.InvariantCulture) || currentCulture.Equals(currentCulture.Parent))
            {
                break;
            }

            currentCulture = currentCulture.Parent;
        }

        return path;
    }
}
