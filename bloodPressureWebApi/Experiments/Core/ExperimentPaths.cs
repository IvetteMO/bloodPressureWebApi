namespace bloodPressureWebApi.Experiments.Core;

public static class ExperimentPaths
{
    public static string CreateOutputFolder(string experiment)
    {
        string projectRoot =
            Directory.GetParent(AppContext.BaseDirectory)!
                .Parent!
                .Parent!
                .Parent!
                .FullName;

        string folder =
            Path.Combine(
                projectRoot,
                "Experiments",
                "Results",
                experiment,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        Directory.CreateDirectory(folder);

        return folder;
    }
}