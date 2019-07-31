using System;

namespace SS14.Launcher
{
#pragma warning disable 649
    [Serializable]
    internal class JenkinsJobInfo
    {
        public JenkinsBuildRef LastSuccessfulBuild;
    }

    [Serializable]
    internal class JenkinsBuildRef
    {
        public int Number;
        public string Url;
    }
#pragma warning restore 649
}
