using System;

namespace Content.MapRenderer.Viewer
{
    public class WebsiteViewer
    {
        private const string WebsiteUrl = "https://spacestation14.io/map-diff-viewer/";

        public string From(string oldLink, string newLink)
        {
            oldLink = Uri.EscapeDataString(oldLink);
            newLink = Uri.EscapeDataString(newLink);

            return $"{WebsiteUrl}?old={oldLink}&new={newLink}";
        }
    }
}
